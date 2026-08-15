using ECommerce.Domain.Audit;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Options;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace ECommerce.UseCases.Payments.Handlers;

/// <summary>
/// Executes an approved refund idempotently through the originating provider. The provider key is the
/// refund id, so concurrent or repeated execution never produces a duplicate provider refund (QAS-04).
/// On success the payment is marked refunded (triggering credit notes), returned items are restocked
/// atomically, and on failure the refund is flagged for the retry job.
/// </summary>
public sealed class ExecuteRefundCommandHandler(
    IRefundRepository refunds,
    IPaymentRepository payments,
    IOrderRepository orders,
    IPaymentProviderFactory providerFactory,
    IPaymentProviderHealth health,
    IStockAllocator stockAllocator,
    IRefundRetryJobScheduler retryScheduler,
    IAuditLogWriter auditLogWriter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<RefundRetryOptions> retryOptions,
    IValidator<ExecuteRefundCommand> validator) : IRequestHandler<ExecuteRefundCommand, Result<RefundResponse>>
{
    public async Task<Result<RefundResponse>> Handle(ExecuteRefundCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<RefundResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var refund = await refunds.GetByIdAsync(request.RefundId, cancellationToken);
        if (refund is null)
        {
            return RefundErrors.RefundNotFound;
        }

        // Idempotent replay: an already-completed refund returns the stored response (QAS-04).
        if (refund.Status == RefundStatus.Completed)
        {
            return RefundResponse.From(refund, 0m);
        }

        if (refund.Status is not (RefundStatus.Approved or RefundStatus.Failed))
        {
            return RefundErrors.InvalidState;
        }

        var payment = await payments.GetByIdAsync(refund.PaymentId, cancellationToken);
        if (payment is null)
        {
            return PaymentErrors.PaymentNotFound;
        }

        var begin = refund.BeginExecution(utcNow);
        if (begin.IsFailure)
        {
            return begin.Error;
        }

        IPaymentProvider provider;
        try
        {
            provider = await providerFactory.GetAsync(payment.ProviderKey, cancellationToken);
        }
        catch (Exception)
        {
            return await FailAsync(refund, "provider_unavailable", cancellationToken);
        }

        PaymentRefundResult result;
        try
        {
            result = await provider.RefundAsync(
                new PaymentRefundRequest(
                    refund.Amount,
                    payment.Currency,
                    payment.ProviderReference ?? string.Empty,
                    refund.Id.ToString("N")),
                cancellationToken);
        }
        catch (Exception)
        {
            health.RecordFailure(provider.Key);
            return await FailAsync(refund, "provider_unavailable", cancellationToken);
        }

        if (!result.IsSuccess)
        {
            health.RecordFailure(provider.Key);
            return await FailAsync(refund, result.ErrorCode ?? "refund_failed", cancellationToken);
        }

        health.RecordSuccess(provider.Key);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var completed = refund.MarkCompleted(result.ProviderReference, utcNow);
        if (completed.IsFailure)
        {
            return completed.Error;
        }

        // RequestRefund (Captured → Refunding) then MarkRefunded raises PaymentRefunded, which
        // triggers credit-note issuance exactly once (existing handler is idempotent by refund id).
        var requestRefund = payment.RequestRefund(refund.Reason, utcNow);
        if (requestRefund.IsFailure)
        {
            return requestRefund.Error;
        }

        var markRefunded = payment.MarkRefunded(utcNow, result.ProviderReference);
        if (markRefunded.IsFailure)
        {
            return markRefunded.Error;
        }

        if (refund.Restock)
        {
            var restock = await RestockAsync(refund, cancellationToken);
            if (restock.IsFailure)
            {
                return restock.Error;
            }
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.RefundExecuted,
            "Refund",
            refund.Id.ToString(),
            After: new
            {
                refund.Amount,
                refund.Currency,
                ProviderReference = refund.ProviderReference,
                refund.OrderId,
                refund.PaymentId
            }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RefundResponse.From(refund, 0m);
    }

    private async Task<Result<RefundResponse>> FailAsync(Refund refund, string detail, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var failed = refund.MarkFailed(detail, utcNow);
        if (failed.IsFailure)
        {
            return failed.Error;
        }

        if (refund.Attempts < retryOptions.Value.MaxAttempts)
        {
            retryScheduler.EnqueueRetry(refund.Id);
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.RefundFailedAction,
            "Refund",
            refund.Id.ToString(),
            After: new { refund.Amount, Detail = detail, refund.Attempts }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RefundResponse.From(refund, 0m);
    }

    private async Task<Result> RestockAsync(Refund refund, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(refund.OrderId, cancellationToken);
        if (order is null)
        {
            return RefundErrors.OrderNotFound;
        }

        var skuByProductId = order.Items.ToDictionary(item => item.ProductId, item => item.Sku);
        var items = refund.Items
            .Where(line => skuByProductId.ContainsKey(line.ProductId))
            .Select(line => new AllocationRequestItem(skuByProductId[line.ProductId], line.Quantity))
            .ToList();

        if (items.Count == 0)
        {
            return Result.Success();
        }

        await stockAllocator.ReleaseAsync(
            items,
            "REFUND",
            refund.Id.ToString("N"),
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return Result.Success();
    }
}

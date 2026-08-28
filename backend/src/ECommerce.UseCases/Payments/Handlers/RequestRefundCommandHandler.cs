using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Handlers;

/// <summary>
/// Creates a refund request against an order. The amount must not exceed the payment's
/// remaining refundable balance (paid − already refunded), and the idempotency key makes the create
/// replay-safe.
/// </summary>
public sealed class RequestRefundCommandHandler(
    IOrderRepository orders,
    IPaymentRepository payments,
    IRefundRepository refunds,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<RequestRefundCommand> validator) : IRequestHandler<RequestRefundCommand, Result<RefundResponse>>
{
    public async Task<Result<RefundResponse>> Handle(RequestRefundCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<RefundResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var order = await orders.GetByNumberAsync(request.OrderNumber, cancellationToken);
        if (order is null)
        {
            return RefundErrors.OrderNotFound;
        }

        // Idempotency: a repeated create with the same key replays the stored refund.
        var existing = await refunds.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.OrderId != order.Id)
            {
                return RefundErrors.IdempotencyKeyReuse;
            }

            var replayRefundable = await GetRefundableAmountAsync(existing.PaymentId, cancellationToken);
            return RefundResponse.From(existing, replayRefundable);
        }

        var payment = await payments.GetByIdAsync(order.PaymentId, cancellationToken);
        if (payment is null)
        {
            return PaymentErrors.PaymentNotFound;
        }

        if (payment.Status != PaymentStatus.Captured)
        {
            return RefundErrors.InvalidState;
        }

        var refundable = await GetRefundableAmountAsync(payment.Id, cancellationToken);
        if (request.Amount > refundable)
        {
            return RefundErrors.ExceedsRefundable;
        }

        var items = (request.Items ?? [])
            .Select(item => RefundItem.Create(Guid.Empty, item.ProductId, item.Quantity))
            .ToList();

        var refund = Refund.Create(
            order.Id,
            payment.Id,
            request.Amount,
            payment.Currency,
            request.Reason,
            request.Restock,
            request.IdempotencyKey,
            items,
            utcNow);

        refunds.Add(refund);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RefundResponse.From(refund, refundable);
    }

    private async Task<decimal> GetRefundableAmountAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return 0m;
        }

        var refundsForPayment = await refunds.GetByPaymentIdAsync(paymentId, cancellationToken);
        var alreadyRefunded = refundsForPayment
            .Where(refund => refund.Status is not RefundStatus.Rejected)
            .Sum(refund => refund.Amount);

        return payment.Amount - alreadyRefunded;
    }
}

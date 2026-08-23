using ECommerce.Domain.Payments;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class HandleStripeRefundHandler(
    IPaymentRepository payments,
    IRefundRepository refunds,
    IUnitOfWork unitOfWork) : IRequestHandler<HandleStripeRefundCommand, Result>
{
    public async Task<Result> Handle(HandleStripeRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByProviderTokenAsync(request.PaymentIntentId, cancellationToken);
        if (payment is null)
        {
            return Result.Success();
        }

        if (payment.OrderId is null)
        {
            return Result.Success();
        }

        var utcNow = DateTime.UtcNow;

        var refund = Refund.Create(
            payment.OrderId.Value,
            payment.Id,
            request.AmountRefunded,
            payment.Currency,
            request.Reason,
            false,
            request.ChargeId ?? Guid.NewGuid().ToString("N"),
            [],
            utcNow);

        refund.MarkCompleted(request.ChargeId, utcNow);
        refunds.Add(refund);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class HandleStripePaymentSucceededHandler(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork) : IRequestHandler<HandleStripePaymentSucceededCommand, Result>
{
    public async Task<Result> Handle(HandleStripePaymentSucceededCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByProviderTokenAsync(request.PaymentIntentId, cancellationToken);
        if (payment is null)
        {
            return Result.Success();
        }

        var utcNow = DateTime.UtcNow;

        if (payment.Status is PaymentStatus.Created or PaymentStatus.Failed or PaymentStatus.RetryPending)
        {
            payment.MarkAuthorized(request.PaymentIntentId, utcNow);
        }

        if (payment.Status == PaymentStatus.Authorized)
        {
            payment.Capture(payment.Amount, utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

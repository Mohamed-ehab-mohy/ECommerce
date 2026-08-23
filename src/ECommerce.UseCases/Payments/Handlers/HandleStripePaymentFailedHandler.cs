using ECommerce.Domain.Payments;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.UseCases.Payments.Handlers;

public sealed class HandleStripePaymentFailedHandler(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork) : IRequestHandler<HandleStripePaymentFailedCommand, Result>
{
    public async Task<Result> Handle(HandleStripePaymentFailedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByProviderTokenAsync(request.PaymentIntentId, cancellationToken);
        if (payment is null)
        {
            return Result.Success();
        }

        var utcNow = DateTime.UtcNow;
        payment.MarkFailed(utcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

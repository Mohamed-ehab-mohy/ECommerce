using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Checkout.Queries;
using ECommerce.UseCases.Checkout.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.UseCases.Checkout.Handlers;

public sealed class GetCheckoutQueryHandler(
    ICheckoutRepository checkouts,
    IPaymentRepository payments,
    ICurrentUser currentUser) : IRequestHandler<GetCheckoutQuery, Result<CheckoutResponse>>
{
    public async Task<Result<CheckoutResponse>> Handle(GetCheckoutQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
        {
            return CheckoutErrors.CheckoutUnauthorized;
        }

        var checkout = await checkouts.GetByIdAsync(request.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return CheckoutErrors.CheckoutNotFound;
        }

        // A registered checkout may only be read by its owner.
        // Guest checkouts (no customer id) are never exposed through this authenticated handler.
        if (!checkout.CustomerId.HasValue || checkout.CustomerId != currentUser.UserId.Value)
        {
            return CheckoutErrors.CheckoutUnauthorized;
        }

        if (checkout.PaymentId is not { } paymentId)
        {
            return CheckoutErrors.InvalidState;
        }

        var payment = await payments.GetByIdAsync(paymentId, cancellationToken);
        return payment is null ? PaymentErrors.PaymentNotFound : CheckoutResponseFactory.From(checkout, payment);
    }
}

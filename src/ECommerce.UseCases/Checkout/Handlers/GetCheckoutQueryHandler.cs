using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Checkout.Queries;
using ECommerce.UseCases.Checkout.Responses;
using ECommerce.UseCases.Payments.Ports;
using MediatR;

namespace ECommerce.UseCases.Checkout.Handlers;

public sealed class GetCheckoutQueryHandler(
    ICheckoutRepository checkouts,
    IPaymentRepository payments) : IRequestHandler<GetCheckoutQuery, Result<CheckoutResponse>>
{
    public async Task<Result<CheckoutResponse>> Handle(GetCheckoutQuery request, CancellationToken cancellationToken)
    {
        var checkout = await checkouts.GetByIdAsync(request.CheckoutId, cancellationToken);
        if (checkout is null)
        {
            return CheckoutErrors.CheckoutNotFound;
        }

        if (checkout.PaymentId is not { } paymentId)
        {
            return CheckoutErrors.InvalidState;
        }

        var payment = await payments.GetByIdAsync(paymentId, cancellationToken);
        return payment is null ? PaymentErrors.PaymentNotFound : CheckoutResponseFactory.From(checkout, payment);
    }
}

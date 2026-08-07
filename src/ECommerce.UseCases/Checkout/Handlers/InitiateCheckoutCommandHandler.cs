using ECommerce.Domain.Cart;
using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Ports;
using ECommerce.UseCases.Checkout.Commands;
using ECommerce.UseCases.Checkout.Ports;
using ECommerce.UseCases.Checkout.Responses;
using ECommerce.UseCases.Checkout.Services;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Services;
using FluentValidation;
using MediatR;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UseCases.Checkout.Handlers;

public sealed class InitiateCheckoutCommandHandler(
    ICartRepository carts,
    ICheckoutRepository checkouts,
    IPaymentRepository payments,
    PaymentIntentService paymentIntents,
    CheckoutTotalsCalculator totalsCalculator,
    StockAvailabilityVerifier availabilityVerifier,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<InitiateCheckoutCommand> validator) : IRequestHandler<InitiateCheckoutCommand, Result<CheckoutResponse>>
{
    public async Task<Result<CheckoutResponse>> Handle(InitiateCheckoutCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<CheckoutResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var shippingAddress = ToSnapshot(request.ShippingAddress);
        var billingAddress = request.BillingAddress is null ? null : ToSnapshot(request.BillingAddress);

        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken);
        if (cart is null)
        {
            return CartErrors.CartNotFound;
        }

        if (cart.Items.Count == 0)
        {
            return CheckoutErrors.CartEmpty;
        }

        var lines = cart.Items
            .Select(item => new PriceSnapshotItem(
                item.ProductId,
                item.Sku,
                item.Name,
                item.ListPrice,
                item.UnitPrice,
                item.Quantity,
                item.ImageUrl))
            .ToList();

        var totalsResult = await totalsCalculator.ComputeAsync(
            lines,
            request.ShippingMethodId,
            shippingAddress.Country,
            request.Currency,
            cancellationToken);
        if (totalsResult.IsFailure)
        {
            return totalsResult.Error;
        }

        var issues = await availabilityVerifier.VerifyAsync(cart.Items, cancellationToken);
        if (issues.Count > 0)
        {
            return CheckoutErrors.InsufficientStock(
                issues.Select(issue => new StockShortageLine(issue.Sku, issue.Requested, issue.Available)).ToList());
        }

        var paymentResult = await paymentIntents.CreateIntentAsync(
            request.CustomerId,
            request.ProviderKey,
            request.MethodType,
            request.Currency,
            request.Country,
            totalsResult.Value.GrandTotal,
            cancellationToken);
        if (paymentResult.IsFailure)
        {
            return paymentResult.Error;
        }

        var checkout = CheckoutAggregate.Create(
            cart.Id,
            request.CustomerId,
            request.CustomerEmail,
            request.Currency,
            new PriceSnapshot(lines, new TotalsSnapshot(
                totalsResult.Value.Subtotal,
                totalsResult.Value.ItemDiscount,
                totalsResult.Value.CartDiscount,
                totalsResult.Value.ShippingTotal,
                totalsResult.Value.TaxTotal,
                totalsResult.Value.GrandTotal)),
            shippingAddress,
            billingAddress ?? shippingAddress,
            request.ShippingMethodId,
            paymentResult.Value.Payment.Id,
            utcNow.AddMinutes(30),
            utcNow);

        payments.Add(paymentResult.Value.Payment);
        checkouts.Add(checkout);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CheckoutResponseFactory.From(checkout, paymentResult.Value.Payment);
    }

    private static AddressSnapshot ToSnapshot(AddressInput address) =>
        new(
            address.FullName,
            address.Phone,
            address.Street,
            address.City,
            address.Region,
            address.Country,
            address.PostalCode);
}

using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UseCases.Checkout.Responses;

public sealed record CheckoutTotalsResponse(
    decimal Subtotal,
    decimal ItemDiscount,
    decimal CartDiscount,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Currency);

public sealed record PaymentInitiationResponse(string ClientToken, string ProviderKey, Guid PaymentId);

public sealed record CheckoutResponse(
    Guid CheckoutId,
    Guid CartId,
    string Currency,
    CheckoutStatus Status,
    IReadOnlyList<PriceSnapshotItem> Lines,
    CheckoutTotalsResponse Totals,
    PaymentInitiationResponse Payment,
    string CapabilityToken,
    DateTime ExpiresAt);

public static class CheckoutResponseFactory
{
    public static CheckoutResponse From(CheckoutAggregate checkout, Payment payment) =>
        new(
            checkout.Id,
            checkout.CartId,
            checkout.Currency,
            checkout.Status,
            checkout.PriceSnapshot.Lines,
            new CheckoutTotalsResponse(
                checkout.PriceSnapshot.Totals.Subtotal,
                checkout.PriceSnapshot.Totals.ItemDiscount,
                checkout.PriceSnapshot.Totals.CartDiscount,
                checkout.PriceSnapshot.Totals.ShippingTotal,
                checkout.PriceSnapshot.Totals.TaxTotal,
                checkout.PriceSnapshot.Totals.GrandTotal,
                checkout.Currency),
            new PaymentInitiationResponse(payment.ClientToken, payment.ProviderKey, payment.Id),
            checkout.CapabilityToken,
            checkout.ExpiresAt);
}

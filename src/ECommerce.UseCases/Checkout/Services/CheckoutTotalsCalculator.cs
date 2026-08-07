using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.UseCases.Checkout.Services;

public sealed record CheckoutTotals(
    decimal Subtotal,
    decimal ItemDiscount,
    decimal CartDiscount,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal);

public sealed class CheckoutTotalsCalculator(IShippingRateProvider shippingRates, ITaxCalculator taxCalculator)
{
    public async Task<Result<CheckoutTotals>> ComputeAsync(
        IReadOnlyCollection<PriceSnapshotItem> lines,
        string shippingMethodId,
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        var subtotal = lines.Sum(line => line.ListPrice * line.Quantity);
        var itemDiscount = lines.Sum(line => (line.ListPrice - line.UnitPrice) * line.Quantity);
        var cartDiscount = 0m;

        var shipping = await shippingRates.GetRateAsync(shippingMethodId, country, currency, cancellationToken);
        if (shipping is null)
        {
            return CheckoutErrors.ShippingMethodUnsupported;
        }

        var taxable = subtotal - itemDiscount - cartDiscount;
        var tax = await taxCalculator.ComputeAsync(taxable, country, currency, cancellationToken);
        var grandTotal = taxable + shipping.Rate + tax;

        return new CheckoutTotals(subtotal, itemDiscount, cartDiscount, shipping.Rate, tax, grandTotal);
    }
}

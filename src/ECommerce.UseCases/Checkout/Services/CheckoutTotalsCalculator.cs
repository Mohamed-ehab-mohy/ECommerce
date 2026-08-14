using ECommerce.Domain.Orders;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Ports;

namespace ECommerce.UseCases.Checkout.Services;

public sealed record CheckoutTotals(
    decimal Subtotal,
    decimal ItemDiscount,
    decimal CartDiscount,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    decimal TaxRate);

/// <summary>Promotion-aware totals with the applied rule ids snapshot (T-DAT-009).</summary>
public sealed record PromotionAwareTotals(
    CheckoutTotals Totals,
    IReadOnlyList<Guid> AppliedPromotionIds,
    Guid? AppliedCouponId);

public sealed record ProductLineAttributes(IReadOnlyList<Guid> CategoryIds, IReadOnlyList<Guid> BrandIds)
{
    public static readonly ProductLineAttributes Empty = new([], []);
}

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
        var grandTotal = taxable + shipping.Rate + tax.Amount;

        return new CheckoutTotals(subtotal, itemDiscount, cartDiscount, shipping.Rate, tax.Amount, grandTotal, tax.Rate);
    }

    /// <summary>
    /// Computes totals through the domain <see cref="PricingEngine"/> (FRS-E-004/005). Ordering consumes this
    /// result and never reimplements discount math.
    /// </summary>
    public async Task<Result<PromotionAwareTotals>> ComputePromotionAwareAsync(
        IReadOnlyCollection<PriceSnapshotItem> lines,
        IReadOnlyDictionary<Guid, ProductLineAttributes> productAttributes,
        IReadOnlyList<Promotion> promotions,
        Coupon? coupon,
        string customerSegment,
        DateTime utcNow,
        string shippingMethodId,
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        var shipping = await shippingRates.GetRateAsync(shippingMethodId, country, currency, cancellationToken);
        if (shipping is null)
        {
            return CheckoutErrors.ShippingMethodUnsupported;
        }

        var pricingLines = lines
            .Select(line =>
            {
                var attributes = productAttributes.GetValueOrDefault(line.ProductId, ProductLineAttributes.Empty);
                return new PricingLine(
                    line.ProductId,
                    line.Sku,
                    line.ListPrice,
                    line.UnitPrice,
                    line.Quantity,
                    attributes.CategoryIds,
                    attributes.BrandIds);
            })
            .ToList();

        var context = new PricingContext(
            null,
            customerSegment,
            country,
            currency,
            shipping.Rate,
            pricingLines);

        var result = PricingEngine.Evaluate(context, promotions, utcNow, coupon);

        var subtotal = result.Subtotal;
        var baseItemDiscount = lines.Sum(line => (line.ListPrice - line.UnitPrice) * line.Quantity);
        var itemDiscount = baseItemDiscount + result.ItemDiscounts.Sum(discount => discount.Amount);
        var cartDiscount = Math.Min(result.CartDiscount, Math.Max(subtotal - itemDiscount, 0m));
        var shippingTotal = Math.Max(shipping.Rate - result.ShippingDiscount, 0m);

        var taxable = Math.Max(subtotal - itemDiscount - cartDiscount, 0m);
        var tax = await taxCalculator.ComputeAsync(taxable, country, currency, cancellationToken);
        var grandTotal = taxable + shippingTotal + tax.Amount;

        var appliedCouponId = coupon is null ? null : result.AppliedRuleIds.Contains(coupon.PromotionId) ? coupon.Id : (Guid?)null;

        return new PromotionAwareTotals(
            new CheckoutTotals(subtotal, itemDiscount, cartDiscount, shippingTotal, tax.Amount, grandTotal, tax.Rate),
            result.AppliedRuleIds,
            appliedCouponId);
    }
}

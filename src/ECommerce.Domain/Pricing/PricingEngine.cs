namespace ECommerce.Domain.Pricing;

public sealed record PricingLine(
    Guid ProductId,
    string Sku,
    decimal ListPrice,
    decimal UnitPrice,
    int Quantity,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<Guid> BrandIds);

public sealed record PricingContext(
    Guid? CustomerId,
    string CustomerSegment,
    string Country,
    string Currency,
    decimal ShippingRate,
    IReadOnlyList<PricingLine> Lines)
{
    public decimal Subtotal => Lines.Sum(line => line.ListPrice * line.Quantity);

    public int TotalQuantity => Lines.Sum(line => line.Quantity);
}

public sealed record AppliedItemDiscount(Guid ProductId, decimal Amount);

/// <summary>
/// Pricing contract (docs/06c §4.5, Conformist): Ordering consumes this computed result and never
/// reimplements discount math.
/// </summary>
public sealed record PricingResult(
    IReadOnlyList<AppliedItemDiscount> ItemDiscounts,
    decimal CartDiscount,
    decimal ShippingDiscount,
    IReadOnlyList<Guid> AppliedRuleIds,
    decimal Subtotal);

public static class PromotionConditionEvaluator
{
    public static bool Matches(PromotionCondition condition, PricingContext context) => condition switch
    {
        ProductCondition c => context.Lines.Any(line => c.ProductIds.Contains(line.ProductId)),
        CategoryCondition c => context.Lines.Any(line => line.CategoryIds.Any(c.CategoryIds.Contains)),
        BrandCondition c => context.Lines.Any(line => line.BrandIds.Any(c.BrandIds.Contains)),
        MinQuantityCondition c => context.TotalQuantity >= c.MinQuantity,
        MinAmountCondition c => context.Subtotal >= c.MinAmount,
        SegmentCondition c => string.Equals(c.Segment, context.CustomerSegment, StringComparison.OrdinalIgnoreCase),
        _ => false
    };
}

/// <summary>
/// Domain pricing service (docs/06a §5.9). Applies promotions in priority order item → cart → shipping
/// (FRS-E-004), resolves stacking via each promotion's stacking matrix (best-of unless all can stack),
/// and enforces the non-negative totals invariant (FRS-E-005).
/// </summary>
public static class PricingEngine
{
    private sealed record PromotionCandidate(Promotion Promotion, decimal Discount);

    public static PricingResult Evaluate(
        PricingContext context,
        IReadOnlyList<Promotion> promotions,
        DateTime utcNow,
        Coupon? coupon = null)
    {
        var eligible = promotions
            .Where(promotion => promotion.IsEligible(context, utcNow))
            .OrderBy(promotion => promotion.CreatedAt)
            .ThenBy(promotion => promotion.Id)
            .ToList();

        // A promotion redeemed through a coupon is applied only via the coupon, never auto-applied
        // (avoids double counting when both are supplied).
        var autoEligible = coupon is null
            ? eligible
            : eligible.Where(promotion => promotion.Id != coupon.PromotionId).ToList();

        var appliedRuleIds = new HashSet<Guid>();

        // ---- 1. Item-level discounts (priority first) ----
        var itemDiscountsByProduct = new Dictionary<Guid, decimal>();

        var itemTargets = autoEligible
            .Where(promotion => promotion.Actions.Any(action => action.Type == DiscountType.Product))
            .ToDictionary(
                promotion => promotion.Id,
                promotion => promotion.TargetLines(context).Select(line => line.ProductId).ToHashSet());

        foreach (var line in context.Lines)
        {
            var candidates = autoEligible
                .Where(promotion =>
                    promotion.Actions.Any(action => action.Type == DiscountType.Product)
                    && itemTargets.TryGetValue(promotion.Id, out var targetIds)
                    && targetIds.Contains(line.ProductId))
                .Select(promotion => new PromotionCandidate(
                    promotion,
                    promotion.Actions
                        .Where(action => action.Type == DiscountType.Product)
                        .Sum(action => action.ApplyTo(line.ListPrice * line.Quantity))))
                .Where(candidate => candidate.Discount > 0m)
                .ToList();

            var discount = Math.Min(Resolve(candidates), line.ListPrice * line.Quantity);

            if (discount <= 0m)
            {
                continue;
            }

            itemDiscountsByProduct[line.ProductId] = discount;
            ApplyRuleIds(appliedRuleIds, candidates, discount);
        }

        var subtotal = context.Subtotal;
        var itemDiscountTotal = itemDiscountsByProduct.Values.Sum();

        // ---- 2. Cart-level discounts ----
        var orderCandidates = autoEligible
            .Where(promotion => promotion.Actions.Any(action => action.Type == DiscountType.Order))
            .Select(promotion => new PromotionCandidate(
                promotion,
                promotion.Actions
                    .Where(action => action.Type == DiscountType.Order)
                    .Sum(action => action.ApplyTo(subtotal))))
            .Where(candidate => candidate.Discount > 0m)
            .ToList();

        var cartDiscount = Resolve(orderCandidates);
        ApplyRuleIds(appliedRuleIds, orderCandidates, cartDiscount);

        // Coupon stacks with auto-applied promotions (UC-E-003) and is clamped to the remaining subtotal.
        if (coupon is not null && coupon.IsActiveAt(utcNow))
        {
            var couponPromotion = promotions.FirstOrDefault(promotion => promotion.Id == coupon.PromotionId);
            if (couponPromotion is not null && couponPromotion.IsEligible(context, utcNow))
            {
                var couponDiscount = couponPromotion.Actions
                    .Where(action => action.Type == DiscountType.Order)
                    .Sum(action => action.ApplyTo(subtotal));

                cartDiscount += couponDiscount;
                if (couponDiscount > 0m)
                {
                    appliedRuleIds.Add(coupon.PromotionId);
                }
            }
        }

        var remaining = Math.Max(subtotal - itemDiscountTotal, 0m);
        cartDiscount = Math.Min(Math.Max(cartDiscount, 0m), remaining);

        // ---- 3. Shipping-level discounts (last priority) ----
        var shippingCandidates = autoEligible
            .Where(promotion => promotion.Actions.Any(action => action.Type == DiscountType.Shipping))
            .Select(promotion => new PromotionCandidate(
                promotion,
                promotion.Actions
                    .Where(action => action.Type == DiscountType.Shipping)
                    .Sum(action => action.ApplyTo(context.ShippingRate))))
            .Where(candidate => candidate.Discount > 0m)
            .ToList();

        var shippingDiscount = Math.Min(Resolve(shippingCandidates), context.ShippingRate);
        ApplyRuleIds(appliedRuleIds, shippingCandidates, shippingDiscount);

        return new PricingResult(
            itemDiscountsByProduct
                .Select(pair => new AppliedItemDiscount(pair.Key, pair.Value))
                .ToList(),
            cartDiscount,
            shippingDiscount,
            appliedRuleIds.OrderBy(id => id).ToList(),
            subtotal);
    }

    /// <summary>Deterministic stacking resolution: additive only when every promotion can stack with every other, else best-of.</summary>
    private static decimal Resolve(IReadOnlyList<PromotionCandidate> candidates) =>
        candidates.Count switch
        {
            0 => 0m,
            1 => candidates[0].Discount,
            _ => CanStackAll(candidates)
                ? candidates.Sum(candidate => candidate.Discount)
                : candidates.Max(candidate => candidate.Discount)
        };

    private static bool CanStackAll(IReadOnlyList<PromotionCandidate> candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            for (var j = i + 1; j < candidates.Count; j++)
            {
                if (!candidates[i].Promotion.Stacking.CanStackWith(candidates[j].Promotion.Id)
                    || !candidates[j].Promotion.Stacking.CanStackWith(candidates[i].Promotion.Id))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void ApplyRuleIds(
        HashSet<Guid> appliedRuleIds,
        IReadOnlyList<PromotionCandidate> candidates,
        decimal effectiveDiscount)
    {
        if (effectiveDiscount <= 0m || candidates.Count == 0)
        {
            return;
        }

        if (candidates.Count == 1 || CanStackAll(candidates))
        {
            foreach (var candidate in candidates)
            {
                appliedRuleIds.Add(candidate.Promotion.Id);
            }

            return;
        }

        var best = candidates.OrderByDescending(candidate => candidate.Discount).First();
        appliedRuleIds.Add(best.Promotion.Id);
    }
}

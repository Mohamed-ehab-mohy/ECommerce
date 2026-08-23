
namespace ECommerce.Domain.Pricing;

public enum DiscountType
{
    Product,
    Order,
    Shipping
}

public enum DiscountBasis
{
    Amount,
    Percent
}

/// <summary>Discount rule value object used inside promotion actions and the order snapshot (FRS-E-001).</summary>
public sealed record DiscountRule(DiscountType Type, DiscountBasis Basis, decimal Value, decimal? Cap)
{
    /// <summary>PR-1: percent actions ≤ 100%; value > 0; cap ≥ 0.</summary>
    public static Result Validate(DiscountType type, DiscountBasis basis, decimal value, decimal? cap)
    {
        if (value <= 0m)
        {
            return PromotionErrors.InvalidDiscountValue;
        }

        if (basis == DiscountBasis.Percent && value > 100m)
        {
            return PromotionErrors.InvalidDiscountValue;
        }

        if (cap is < 0m)
        {
            return PromotionErrors.InvalidDiscountCap;
        }

        _ = type;
        return Result.Success();
    }

    /// <summary>Applies the rule to a base amount; floors at zero and never exceeds the base (FRS-E-005, edge 8.4).</summary>
    public decimal ApplyTo(decimal baseAmount)
    {
        var discount = Basis == DiscountBasis.Percent
            ? baseAmount * Value / 100m
            : Value;

        if (Cap is not null)
        {
            discount = Math.Min(discount, Cap.Value);
        }

        return Math.Min(Math.Max(discount, 0m), Math.Max(baseAmount, 0m));
    }
}

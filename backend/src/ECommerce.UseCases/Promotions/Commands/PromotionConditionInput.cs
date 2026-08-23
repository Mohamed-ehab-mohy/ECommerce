using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Promotions.Commands;

/// <summary>Discriminated condition input; <see cref="Type"/> selects the condition (product/category/brand/min_qty/min_amount/segment).</summary>
public sealed record PromotionConditionInput(
    string Type,
    IReadOnlyList<Guid>? ProductIds = null,
    IReadOnlyList<Guid>? CategoryIds = null,
    IReadOnlyList<Guid>? BrandIds = null,
    int? MinQuantity = null,
    decimal? MinAmount = null,
    string? Segment = null)
{
    public PromotionCondition ToDomain() => Type.Trim().ToLowerInvariant() switch
    {
        "product" => new ProductCondition(ProductIds ?? []),
        "category" => new CategoryCondition(CategoryIds ?? []),
        "brand" => new BrandCondition(BrandIds ?? []),
        "min_qty" => new MinQuantityCondition(MinQuantity ?? 1),
        "min_amount" => new MinAmountCondition(MinAmount ?? 0m),
        "segment" => new SegmentCondition(Segment ?? string.Empty),
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown promotion condition type.")
    };
}

using System.Text.Json.Serialization;

namespace ECommerce.Domain.Pricing;

/// <summary>Campaign eligibility condition. Serialized to JSON on <c>promotions.conditions</c> (jsonb).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(ProductCondition), "product")]
[JsonDerivedType(typeof(CategoryCondition), "category")]
[JsonDerivedType(typeof(BrandCondition), "brand")]
[JsonDerivedType(typeof(MinQuantityCondition), "min_qty")]
[JsonDerivedType(typeof(MinAmountCondition), "min_amount")]
[JsonDerivedType(typeof(SegmentCondition), "segment")]
public abstract record PromotionCondition;

public sealed record ProductCondition(IReadOnlyList<Guid> ProductIds) : PromotionCondition;

public sealed record CategoryCondition(IReadOnlyList<Guid> CategoryIds) : PromotionCondition;

public sealed record BrandCondition(IReadOnlyList<Guid> BrandIds) : PromotionCondition;

public sealed record MinQuantityCondition(int MinQuantity) : PromotionCondition;

public sealed record MinAmountCondition(decimal MinAmount) : PromotionCondition;

public sealed record SegmentCondition(string Segment) : PromotionCondition;

using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Promotions.Commands;

public sealed record DiscountRuleInput(
    DiscountType Type,
    DiscountBasis Basis,
    decimal Value,
    decimal? Cap)
{
    public DiscountRule ToDomain() => new(Type, Basis, Value, Cap);
}

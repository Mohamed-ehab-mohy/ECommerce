using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Promotions.Responses;

public sealed record PromotionResponse(
    Guid Id,
    string Name,
    string State,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool AllowStack,
    IReadOnlyList<Guid> AllowStackWith,
    IReadOnlyCollection<PromotionCondition> Conditions,
    IReadOnlyCollection<DiscountRule> Actions,
    IReadOnlyCollection<string> EligibleCountries,
    IReadOnlyCollection<string> EligibleCurrencies,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PromotionResponse From(Promotion promotion) =>
        new(
            promotion.Id,
            promotion.Name,
            promotion.State.ToString(),
            promotion.StartsAt,
            promotion.EndsAt,
            promotion.Stacking.AllowStack,
            promotion.Stacking.AllowStackWith,
            promotion.Conditions,
            promotion.Actions,
            promotion.EligibleCountries,
            promotion.EligibleCurrencies,
            promotion.CreatedAt,
            promotion.UpdatedAt);
}

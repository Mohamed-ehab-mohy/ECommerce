using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Promotions.Commands;

namespace ECommerce.API.Controllers;

public sealed record CreatePromotionRequest(
    string Name,
    IReadOnlyList<PromotionConditionInput>? Conditions,
    IReadOnlyList<DiscountRuleInput>? Actions,
    bool AllowStack,
    IReadOnlyList<Guid>? AllowStackWith,
    IReadOnlyList<string>? EligibleCountries,
    IReadOnlyList<string>? EligibleCurrencies,
    DateTime? StartsAt,
    DateTime? EndsAt);

public sealed record UpdatePromotionRequest(
    string Name,
    IReadOnlyList<PromotionConditionInput>? Conditions,
    IReadOnlyList<DiscountRuleInput>? Actions,
    bool AllowStack,
    IReadOnlyList<Guid>? AllowStackWith,
    IReadOnlyList<string>? EligibleCountries,
    IReadOnlyList<string>? EligibleCurrencies);

public sealed record SchedulePromotionRequest(DateTime? StartsAt, DateTime? EndsAt);

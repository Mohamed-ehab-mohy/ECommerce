using ECommerce.Domain.Pricing;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Responses;

namespace ECommerce.UseCases.Promotions.Commands;

public sealed record CreatePromotionCommand(
    string Name,
    IReadOnlyList<PromotionConditionInput> Conditions,
    IReadOnlyList<DiscountRuleInput> Actions,
    bool AllowStack,
    IReadOnlyList<Guid> AllowStackWith,
    IReadOnlyList<string> EligibleCountries,
    IReadOnlyList<string> EligibleCurrencies,
    DateTime? StartsAt,
    DateTime? EndsAt) : IRequest<Result<PromotionResponse>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsWrite;

    public StackingMatrix ToStacking() => new(AllowStack, AllowStackWith ?? []);
}

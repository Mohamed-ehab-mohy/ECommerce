using ECommerce.Domain.Pricing;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Responses;
using MediatR;

namespace ECommerce.UseCases.Promotions.Commands;

public sealed record UpdatePromotionCommand(
    Guid Id,
    string Name,
    IReadOnlyList<PromotionConditionInput> Conditions,
    IReadOnlyList<DiscountRuleInput> Actions,
    bool AllowStack,
    IReadOnlyList<Guid> AllowStackWith,
    IReadOnlyList<string> EligibleCountries,
    IReadOnlyList<string> EligibleCurrencies) : IRequest<Result<PromotionResponse>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsWrite;

    public StackingMatrix ToStacking() => new(AllowStack, AllowStackWith ?? []);
}

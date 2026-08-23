using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Responses;

namespace ECommerce.UseCases.Promotions.Commands;

public sealed record ActivatePromotionCommand(Guid Id)
    : IRequest<Result<PromotionResponse>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsWrite;
}

public sealed record PausePromotionCommand(Guid Id)
    : IRequest<Result<PromotionResponse>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsWrite;
}

public sealed record SchedulePromotionCommand(
    Guid Id,
    DateTime? StartsAt,
    DateTime? EndsAt) : IRequest<Result<PromotionResponse>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsWrite;
}

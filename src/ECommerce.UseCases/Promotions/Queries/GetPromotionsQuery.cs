using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Responses;
using MediatR;

namespace ECommerce.UseCases.Promotions.Queries;

public sealed record GetPromotionsQuery : IRequest<Result<IReadOnlyList<PromotionResponse>>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsRead;
}

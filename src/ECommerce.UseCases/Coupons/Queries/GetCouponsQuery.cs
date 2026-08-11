using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Coupons.Responses;
using MediatR;

namespace ECommerce.UseCases.Coupons.Queries;

public sealed record GetCouponsQuery : IRequest<Result<IReadOnlyList<CouponResponse>>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsRead;
}

using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Coupons.Responses;

namespace ECommerce.UseCases.Coupons.Queries;

public sealed record GetCouponsQuery : IRequest<Result<IReadOnlyList<CouponResponse>>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsRead;
}

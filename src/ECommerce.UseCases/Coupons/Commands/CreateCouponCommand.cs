using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Coupons.Responses;

namespace ECommerce.UseCases.Coupons.Commands;

public sealed record CreateCouponCommand(
    string Code,
    Guid PromotionId,
    int TotalUses,
    int? PerCustomerLimit,
    DateTime? StartsAt,
    DateTime? EndsAt) : IRequest<Result<CouponResponse>>, IRequirePermission
{
    public string Permission => Permissions.PromotionsWrite;
}

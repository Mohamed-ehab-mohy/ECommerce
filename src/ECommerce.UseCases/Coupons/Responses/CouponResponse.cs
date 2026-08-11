using ECommerce.Domain.Pricing;
using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Coupons.Responses;

public sealed record CouponResponse(
    Guid Id,
    string Code,
    Guid PromotionId,
    int TotalUses,
    int UsedCount,
    int? PerCustomerLimit,
    DateTime? StartsAt,
    DateTime? EndsAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static CouponResponse From(Coupon coupon) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.PromotionId,
            coupon.TotalUses,
            coupon.UsedCount,
            coupon.PerCustomerLimit,
            coupon.StartsAt,
            coupon.EndsAt,
            coupon.CreatedAt,
            coupon.UpdatedAt);
}

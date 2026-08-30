using ECommerce.Domain.Pricing;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Promotions.Ports;

namespace ECommerce.Infrastructure.Promotions;

public sealed class CouponRepository(ECommerceDbContext dbContext) : ICouponRepository
{
    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Set<Coupon>().SingleOrDefaultAsync(
            coupon => coupon.Code == code.Trim().ToUpperInvariant(),
            cancellationToken);

    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Coupon>().SingleOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<Coupon>()
            .AsNoTracking()
            .OrderBy(coupon => coupon.Code)
            .ToListAsync(cancellationToken);

    public Task<int> GetRedemptionCountAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken) =>
        dbContext.Set<CouponUsage>().CountAsync(
            usage => usage.CouponId == couponId && usage.CustomerId == customerId,
            cancellationToken);

    public async Task<CouponRedemptionResult> TryRedeemAsync(
        Guid couponId,
        Guid orderId,
        Guid customerId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (dbContext.CurrentTenant is not { } tenantId)
        {
            return CouponRedemptionResult.Exhausted;
        }

        var updated = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE coupons
            SET used_count = used_count + 1, updated_at = {utcNow}
            WHERE id = {couponId} AND tenant_id = {tenantId}
              AND used_count < total_uses
              AND (per_customer_limit IS NULL OR
                   (SELECT COUNT(*) FROM coupon_usages u WHERE u.coupon_id = {couponId} AND u.customer_id = {customerId})
                       < per_customer_limit)
            """, cancellationToken);

        if (updated == 0)
        {
            var alreadyApplied = await dbContext.Set<CouponUsage>()
                .AnyAsync(usage => usage.CouponId == couponId && usage.OrderId == orderId, cancellationToken);

            return alreadyApplied
                ? CouponRedemptionResult.AlreadyApplied
                : CouponRedemptionResult.Exhausted;
        }

        dbContext.Set<CouponUsage>().Add(
            new CouponUsage(Guid.NewGuid(), couponId, orderId, customerId, utcNow));

        return CouponRedemptionResult.Redeemed;
    }

    public void Add(Coupon coupon) => dbContext.Set<Coupon>().Add(coupon);
}

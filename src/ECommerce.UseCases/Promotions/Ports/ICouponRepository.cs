using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Promotions.Ports;

public enum CouponRedemptionResult
{
    Redeemed,
    Exhausted,
    AlreadyApplied
}

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken);

    Task<int> GetRedemptionCountAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims a coupon use: <c>UPDATE ... WHERE used_count &lt; total_uses</c> within the caller's
    /// transaction (QAS-02). Returns <see cref="CouponRedemptionResult.AlreadyApplied"/> when the order already
    /// redeemed the coupon.
    /// </summary>
    Task<CouponRedemptionResult> TryRedeemAsync(
        Guid couponId,
        Guid orderId,
        Guid customerId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    void Add(Coupon coupon);
}

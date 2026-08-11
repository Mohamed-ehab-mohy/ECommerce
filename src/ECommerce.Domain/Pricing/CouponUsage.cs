namespace ECommerce.Domain.Pricing;

/// <summary>Audit/dedupe row per coupon redemption (coupon_usages). One row per (coupon, order).</summary>
public sealed class CouponUsage
{
    public CouponUsage(Guid id, Guid couponId, Guid orderId, Guid customerId, DateTime redeemedAt)
    {
        Id = id;
        CouponId = couponId;
        OrderId = orderId;
        CustomerId = customerId;
        RedeemedAt = redeemedAt;
    }

    private CouponUsage()
    {
    }

    public Guid Id { get; private set; }

    public Guid CouponId { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTime RedeemedAt { get; private set; }
}

using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Pricing;

/// <summary>
/// Coupon aggregate. The redemption counter is incremented atomically by the repository
/// (<c>UPDATE ... WHERE used_count &lt; total_uses</c>); <see cref="TryRedeem"/> enforces the domain invariant.
/// </summary>
public sealed class Coupon : BaseEntity<Guid>
{
    private Coupon()
    {
        Code = string.Empty;
    }

    public string Code { get; private set; }

    public Guid PromotionId { get; private set; }

    public int TotalUses { get; private set; }

    public int UsedCount { get; private set; }

    public int? PerCustomerLimit { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? EndsAt { get; private set; }

    public static Result<Coupon> Create(
        string code,
        Guid promotionId,
        int totalUses,
        int? perCustomerLimit,
        DateTime? startsAt,
        DateTime? endsAt,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return CouponErrors.CodeRequired;
        }

        if (totalUses <= 0)
        {
            return CouponErrors.InvalidTotalUses;
        }

        if (perCustomerLimit is < 1)
        {
            return CouponErrors.InvalidPerCustomerLimit;
        }

        if (startsAt is not null && endsAt is not null && startsAt > endsAt)
        {
            return CouponErrors.InvalidSchedule;
        }

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            PromotionId = promotionId,
            TotalUses = totalUses,
            UsedCount = 0,
            PerCustomerLimit = perCustomerLimit,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        coupon.AddDomainEvent(new CouponCreated(coupon.Id, coupon.Code, promotionId));

        return coupon;
    }

    public bool IsActiveAt(DateTime utcNow) =>
        (StartsAt is null || StartsAt.Value <= utcNow) && (EndsAt is null || EndsAt.Value >= utcNow);

    public Result TryRedeem(DateTime utcNow) =>
        !IsActiveAt(utcNow)
            ? CouponErrors.InvalidSchedule
            : UsedCount >= TotalUses
                ? CouponErrors.Exhausted
                : Result.Success();

    /// <summary>Bookkeeping after the repository confirmed an atomic redemption (used count already incremented in DB).</summary>
    public void RecordRedemption(Guid orderId, Guid customerId, DateTime utcNow)
    {
        UpdatedAt = utcNow;
        AddDomainEvent(new CouponRedeemed(Id, Code, orderId, customerId));
    }
}

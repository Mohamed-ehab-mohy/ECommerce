using ECommerce.Domain.Events;
using ECommerce.Domain.Pricing;

namespace ECommerce.UnitTests;

public sealed class CouponTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid PromotionId = Guid.NewGuid();

    [Fact]
    public void Create_Normalizes_Code_And_Raises_Event()
    {
        var result = Coupon.Create(" save10 ", PromotionId, 100, 1, null, null, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("SAVE10", result.Value.Code);
        Assert.Equal(0, result.Value.UsedCount);
        Assert.Contains(result.Value.DomainEvents, e => e is CouponCreated);
    }

    [Fact]
    public void Create_Rejects_Invalid_Limits()
    {
        Assert.True(Coupon.Create("X", PromotionId, 0, null, null, null, Now).IsFailure);
        Assert.True(Coupon.Create("X", PromotionId, 5, 0, null, null, Now).IsFailure);
        Assert.True(Coupon.Create("", PromotionId, 5, null, null, null, Now).IsFailure);
    }

    [Fact]
    public void Create_Rejects_Inverted_Schedule()
    {
        var result = Coupon.Create("X", PromotionId, 5, null, Now.AddDays(1), Now, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(CouponErrors.InvalidSchedule, result.Error);
    }

    [Fact]
    public void TryRedeem_Fails_When_Exhausted()
    {
        var coupon = Coupon.Create("X", PromotionId, 2, null, null, null, Now).Value;
        SetUsedCount(coupon, 2);

        var outcome = coupon.TryRedeem(Now);

        Assert.True(outcome.IsFailure);
        Assert.Equal(CouponErrors.Exhausted, outcome.Error);
    }

    private static void SetUsedCount(Coupon coupon, int usedCount)
    {
        var field = typeof(Coupon).GetProperty(nameof(Coupon.UsedCount));
        Assert.NotNull(field);
        field.SetValue(coupon, usedCount);
    }

    [Fact]
    public void TryRedeem_Fails_Outside_Dates()
    {
        var result = Coupon.Create("X", PromotionId, 5, null, Now.AddDays(-2), Now.AddDays(-1), Now);
        var coupon = result.Value;

        Assert.True(coupon.TryRedeem(Now).IsFailure);
        Assert.True(coupon.TryRedeem(Now.AddDays(-3)).IsFailure);
        Assert.True(coupon.TryRedeem(Now.AddDays(-2)).IsSuccess);
    }

    [Fact]
    public void RecordRedemption_Raises_Redeemed_Event()
    {
        var coupon = Coupon.Create("X", PromotionId, 5, null, null, null, Now).Value;
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        coupon.RecordRedemption(orderId, customerId, Now.AddMinutes(1));

        var redeemed = coupon.DomainEvents.OfType<CouponRedeemed>().Single();
        Assert.Equal(orderId, redeemed.OrderId);
        Assert.Equal(customerId, redeemed.CustomerId);
    }
}

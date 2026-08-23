using ECommerce.Domain.Events;
using ECommerce.Domain.Pricing;

namespace ECommerce.UnitTests;

public sealed class PromotionTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    private static PricingContext Context(IReadOnlyList<PricingLine>? lines = null) => new(
        Guid.NewGuid(),
        "retail",
        "AE",
        "AED",
        9.90m,
        lines ??
        [
            new PricingLine(Guid.NewGuid(), "SKU-1", 100.00m, 100.00m, 2, [], [])
        ]);

    private static Promotion CreatePromotion(
        string name = "Summer Sale",
        IReadOnlyList<PromotionCondition>? conditions = null,
        IReadOnlyList<DiscountRule>? actions = null,
        DateTime? startsAt = null,
        DateTime? endsAt = null)
    {
        var result = Promotion.Create(
            name,
            conditions ?? [],
            actions ?? [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 10m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            startsAt,
            endsAt,
            Now);

        Assert.True(result.IsSuccess, result.Error.Description);
        return result.Value;
    }

    [Fact]
    public void Create_Starts_In_Draft_And_Raises_Created_Event()
    {
        var promotion = CreatePromotion();

        Assert.Equal(PromotionState.Draft, promotion.State);
        Assert.Contains(promotion.DomainEvents, e => e is PromotionCreated);
    }

    [Fact]
    public void Create_Rejects_Invalid_Percent_Action()
    {
        var result = Promotion.Create(
            "Broken",
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 150m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            null,
            null,
            Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PromotionErrors.InvalidDiscountValue, result.Error);
    }

    [Fact]
    public void Create_Rejects_Missing_Actions()
    {
        var result = Promotion.Create("NoAction", [], [], StackingMatrix.BestOf, [], [], null, null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PromotionErrors.ActionsRequired, result.Error);
    }

    [Fact]
    public void Create_Rejects_Inverted_Schedule()
    {
        var result = Promotion.Create(
            "Bad Schedule",
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 10m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            Now.AddDays(2),
            Now.AddDays(1),
            Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PromotionErrors.InvalidSchedule, result.Error);
    }

    [Fact]
    public void Activate_From_Draft_Sets_Active()
    {
        var promotion = CreatePromotion();

        var result = promotion.Activate(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PromotionState.Active, promotion.State);
        Assert.Contains(promotion.DomainEvents, e => e is PromotionActivated);
    }

    [Fact]
    public void Pause_Requires_Active()
    {
        var promotion = CreatePromotion();

        var result = promotion.Pause(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PromotionErrors.InvalidState, result.Error);
    }

    [Fact]
    public void Pause_And_Activate_Roundtrip()
    {
        var promotion = CreatePromotion();
        promotion.Activate(Now);

        Assert.True(promotion.Pause(Now).IsSuccess);
        Assert.Equal(PromotionState.Paused, promotion.State);
        Assert.Contains(promotion.DomainEvents, e => e is PromotionPaused);

        Assert.True(promotion.Activate(Now).IsSuccess);
        Assert.Equal(PromotionState.Active, promotion.State);
    }

    [Fact]
    public void Schedule_Updates_Dates_And_Raises_Event()
    {
        var promotion = CreatePromotion();
        var start = Now.AddDays(1);
        var end = Now.AddDays(7);

        var result = promotion.Schedule(start, end, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(start, promotion.StartsAt);
        Assert.Equal(end, promotion.EndsAt);
        Assert.Contains(promotion.DomainEvents, e => e is PromotionScheduled);
    }

    [Fact]
    public void Inactive_Promotion_Never_Applies()
    {
        var draft = CreatePromotion();
        var paused = CreatePromotion("Paused");
        paused.Activate(Now);
        paused.Pause(Now);
        var ended = CreatePromotion("Ended");
        ended.Activate(Now);
        ended.End(Now);

        Assert.False(draft.IsEligible(Context(), Now));
        Assert.False(paused.IsEligible(Context(), Now));
        Assert.False(ended.IsEligible(Context(), Now));
    }

    [Fact]
    public void Active_Promotion_Outside_Schedule_Is_Ineligible()
    {
        var promotion = CreatePromotion(startsAt: Now.AddDays(1), endsAt: Now.AddDays(7));
        promotion.Activate(Now);

        Assert.False(promotion.IsEligible(Context(), Now));
        Assert.True(promotion.IsEligible(Context(), Now.AddDays(3)));
        Assert.False(promotion.IsEligible(Context(), Now.AddDays(8)));
    }

    [Fact]
    public void Country_Scope_Filters_Eligibility()
    {
        var result = Promotion.Create(
            "GCC Only",
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 10m, null)],
            StackingMatrix.BestOf,
            ["AE"],
            [],
            null,
            null,
            Now);

        var promotion = result.Value;
        promotion.Activate(Now);

        Assert.True(promotion.IsEligible(Context(), Now));
        Assert.False(promotion.IsEligible(Context() with { Country = "US" }, Now));
    }

    [Fact]
    public void Conditions_Are_All_Required_For_Eligibility()
    {
        var productId = Guid.NewGuid();
        var promotion = CreatePromotion(
            conditions: [new ProductCondition([productId]), new MinQuantityCondition(2)]);

        promotion.Activate(Now);

        var context = Context(
        [
            new PricingLine(productId, "SKU-1", 50.00m, 50.00m, 1, [], [])
        ]);

        Assert.False(promotion.IsEligible(context, Now));

        context = Context(
        [
            new PricingLine(productId, "SKU-1", 50.00m, 50.00m, 2, [], [])
        ]);

        Assert.True(promotion.IsEligible(context, Now));
    }

    [Fact]
    public void Segment_Condition_Must_Match_Customer()
    {
        var promotion = CreatePromotion(conditions: [new SegmentCondition("vip")]);
        promotion.Activate(Now);

        Assert.False(promotion.IsEligible(Context(), Now));
        Assert.True(promotion.IsEligible(Context() with { CustomerSegment = "VIP" }, Now));
    }

    [Fact]
    public void Update_Replaces_Actions_And_Raises_No_New_Events()
    {
        var promotion = CreatePromotion();
        promotion.Activate(Now);

        var result = promotion.Update(
            "Renamed",
            [new MinAmountCondition(100m)],
            [new DiscountRule(DiscountType.Shipping, DiscountBasis.Percent, 100m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", promotion.Name);
        Assert.Single(promotion.Actions);
        Assert.Equal(DiscountType.Shipping, promotion.Actions.Single().Type);
    }
}

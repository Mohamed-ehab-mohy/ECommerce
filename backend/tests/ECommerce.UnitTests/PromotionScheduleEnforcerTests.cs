using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Promotions.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class PromotionScheduleEnforcerTests
{
    private readonly FakePromotionRepository _promotions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly PromotionScheduleEnforcer _enforcer;

    public PromotionScheduleEnforcerTests()
    {
        _enforcer = new PromotionScheduleEnforcer(
            _promotions,
            _unitOfWork,
            _timeProvider,
            NullLogger<PromotionScheduleEnforcer>.Instance);
    }

    private static Promotion CreateDraft(
        string name,
        DateTime? startsAt,
        DateTime? endsAt)
    {
        var result = Promotion.Create(
            name,
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 10m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            startsAt,
            endsAt,
            DateTime.UtcNow);

        Assert.True(result.IsSuccess, result.Error.Description);
        return result.Value;
    }

    [Fact]
    public async Task Activates_Draft_Promotion_Whose_Window_Has_Started()
    {
        var now = DateTime.UtcNow;
        var promotion = CreateDraft("Black Friday", now.AddMinutes(-1), now.AddHours(24));
        _promotions.Add(promotion);

        var result = await _enforcer.EnforceAsync(CancellationToken.None);

        Assert.Equal(1, result.Activated);
        Assert.Equal(0, result.Paused);
        Assert.Equal(PromotionState.Active, promotion.State);
    }

    [Fact]
    public async Task Leaves_Draft_Promotion_Pending_Until_Window_Starts()
    {
        var now = DateTime.UtcNow;
        var promotion = CreateDraft("Not Yet", now.AddMinutes(30), now.AddHours(24));
        _promotions.Add(promotion);

        var result = await _enforcer.EnforceAsync(CancellationToken.None);

        Assert.Equal(0, result.Activated);
        Assert.Equal(PromotionState.Draft, promotion.State);
    }

    [Fact]
    public async Task Pauses_Active_Promotion_Whose_Window_Has_Ended()
    {
        var now = DateTime.UtcNow;
        var promotion = CreateDraft("Expired", now.AddHours(-48), now.AddHours(-1));
        _promotions.Add(promotion);
        promotion.Activate(now.AddHours(-48));

        var result = await _enforcer.EnforceAsync(CancellationToken.None);

        Assert.Equal(0, result.Activated);
        Assert.Equal(1, result.Paused);
        Assert.Equal(PromotionState.Paused, promotion.State);
    }

    [Fact]
    public async Task Never_Overrides_Manual_Pause()
    {
        var now = DateTime.UtcNow;
        var promotion = CreateDraft("Paused Kill-Switch", now.AddHours(-2), now.AddHours(24));
        _promotions.Add(promotion);
        promotion.Activate(now.AddHours(-2));
        promotion.Pause(now.AddHours(-1));

        var result = await _enforcer.EnforceAsync(CancellationToken.None);

        Assert.Equal(0, result.Activated);
        Assert.Equal(0, result.Paused);
        Assert.Equal(PromotionState.Paused, promotion.State);
    }

    [Fact]
    public async Task Does_Not_Activate_Window_When_Schedule_Already_Ended()
    {
        var now = DateTime.UtcNow;
        var promotion = CreateDraft("Fully Past", now.AddHours(-4), now.AddHours(-2));
        _promotions.Add(promotion);

        var result = await _enforcer.EnforceAsync(CancellationToken.None);

        Assert.Equal(0, result.Activated);
        Assert.Equal(PromotionState.Draft, promotion.State);
    }
}

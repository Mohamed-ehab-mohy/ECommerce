using ECommerce.Domain.Audit;
using ECommerce.Domain.Pricing;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Coupons.Commands;
using ECommerce.UseCases.Coupons.Handlers;

namespace ECommerce.UnitTests;

public sealed class CreateCouponCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly FakeCouponRepository _coupons = new();

    private readonly FakePromotionRepository _promotions = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeAuditLogWriter _auditLog = new();

    private Guid CreatePromotion()
    {
        var promotion = Promotion.Create(
            "Summer Sale", [], [new DiscountRule(DiscountType.Product, DiscountBasis.Percent, 20m, null)], new StackingMatrix(false, []), ["EG"], ["EGP"], null, null, UtcNow)
            .Value;
        _promotions.Promotions.Add(promotion);
        return promotion.Id;
    }

    private CreateCouponCommandHandler CreateHandler() =>
        new(
            _coupons,
            _promotions,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new CreateCouponCommandValidator(),
            _auditLog);

    [Fact]
    public async Task Create_Valid_Adds_Coupon_And_Writes_Audit()
    {
        var promotionId = CreatePromotion();

        var result = await CreateHandler().Handle(
            new CreateCouponCommand("save20", promotionId, 100, 1, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("SAVE20", result.Value.Code);
        Assert.Equal(promotionId, result.Value.PromotionId);
        var coupon = Assert.Single(_coupons.Coupons);
        Assert.Equal(100, coupon.TotalUses);
        var operation = Assert.Single(_auditLog.Operations);
        Assert.Equal(AuditActions.CouponCreated, operation.Action);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_Unknown_Promotion_Returns_NotFound()
    {
        var result = await CreateHandler().Handle(
            new CreateCouponCommand("save20", Guid.NewGuid(), 100, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PromotionErrors.PromotionNotFound, result.Error);
        Assert.Empty(_coupons.Coupons);
    }

    [Fact]
    public async Task Create_Invalid_TotalUses_Returns_Validation_Error()
    {
        var promotionId = CreatePromotion();

        var result = await CreateHandler().Handle(
            new CreateCouponCommand("save20", promotionId, 0, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_coupons.Coupons);
    }
}

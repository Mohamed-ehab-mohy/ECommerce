using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Handlers;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging.Abstractions;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UnitTests;

public sealed class ApplyCartCouponCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly FakeCartRepository _carts = new();

    private readonly FakeCouponRepository _coupons = new();

    private readonly NullLogger<ApplyCartCouponCommandHandler> _logger = new();

    private CartAggregate CreateCart(string ownerKey = "user:11111111-1111-1111-1111-111111111111")
    {
        var cart = CartAggregate.Create(ownerKey, "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 20.00m, 1, null, UtcNow);
        return cart;
    }

    private Coupon CreateCoupon(int totalUses = 100, int? perCustomerLimit = null) =>
        Coupon.Create("SAVE10", Guid.NewGuid(), totalUses, perCustomerLimit, null, null, UtcNow).Value;

    private ApplyCartCouponCommandHandler CreateHandler() =>
        new(
            _carts,
            _coupons,
            new DefaultCurrencyCatalog(),
            new FixedTimeProvider(UtcNow),
            new ApplyCartCouponCommandValidator(),
            _logger);

    [Fact]
    public async Task Apply_Success_Stores_Code()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        _coupons.Coupons.Add(CreateCoupon());

        var result = await CreateHandler().Handle(
            new ApplyCartCouponCommand(cart.OwnerKey, "save10"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("SAVE10", result.Value.AppliedCouponCode);
    }

    [Fact]
    public async Task Apply_Anonymous_Cart_Returns_Customer_Required()
    {
        var cart = CreateCart("anon:abc");
        _carts.Carts.Add(cart);
        _coupons.Coupons.Add(CreateCoupon());

        var result = await CreateHandler().Handle(
            new ApplyCartCouponCommand(cart.OwnerKey, "SAVE10"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CouponErrors.CustomerRequired, result.Error);
    }

    [Fact]
    public async Task Apply_Unknown_Coupon_Returns_NotFound()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);

        var result = await CreateHandler().Handle(
            new ApplyCartCouponCommand(cart.OwnerKey, "NOPE"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CouponErrors.CouponNotFound, result.Error);
    }

    [Fact]
    public async Task Apply_Inactive_Coupon_Returns_Inactive()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        _coupons.Coupons.Add(Coupon.Create(
            "OLD", Guid.NewGuid(), 100, null, UtcNow.AddDays(-10), UtcNow.AddDays(-5), UtcNow).Value);

        var result = await CreateHandler().Handle(
            new ApplyCartCouponCommand(cart.OwnerKey, "OLD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CouponErrors.Inactive, result.Error);
    }

    [Fact]
    public async Task Apply_Exhausted_Coupon_Returns_Exhausted()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        var coupon = CreateCoupon(totalUses: 1);
        typeof(Coupon).GetProperty(nameof(Coupon.UsedCount))!
            .SetValue(coupon, 1);
        _coupons.Coupons.Add(coupon);

        var result = await CreateHandler().Handle(
            new ApplyCartCouponCommand(cart.OwnerKey, "SAVE10"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CouponErrors.Exhausted, result.Error);
    }
}

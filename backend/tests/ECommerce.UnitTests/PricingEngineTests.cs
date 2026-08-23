using ECommerce.Domain.Pricing;

namespace ECommerce.UnitTests;

public sealed class PricingEngineTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();

    private static PricingLine Line(Guid productId, decimal listPrice, int quantity, decimal unitPrice = 0m) => new(
        productId,
        $"SKU-{productId:N}",
        listPrice,
        unitPrice == 0m ? listPrice : unitPrice,
        quantity,
        [],
        []);

    private static PricingContext Context(IReadOnlyList<PricingLine>? lines = null, decimal shippingRate = 9.90m) => new(
        Guid.NewGuid(),
        "retail",
        "AE",
        "AED",
        shippingRate,
        lines ?? [Line(ProductA, 100.00m, 1)]);

    private static Promotion PromotionOf(
        string name,
        DiscountType type,
        DiscountBasis basis,
        decimal value,
        decimal? cap = null,
        StackingMatrix? stacking = null,
        IReadOnlyList<PromotionCondition>? conditions = null,
        IReadOnlyList<string>? countries = null,
        IReadOnlyList<string>? currencies = null)
    {
        var result = Promotion.Create(
            name,
            conditions ?? [],
            [new DiscountRule(type, basis, value, cap)],
            stacking ?? StackingMatrix.BestOf,
            countries ?? [],
            currencies ?? [],
            null,
            null,
            Now);

        Assert.True(result.IsSuccess, result.Error.Description);
        result.Value.Activate(Now);
        return result.Value;
    }

    private static Coupon CouponOf(Guid promotionId, int totalUses = 5) =>
        Coupon.Create("SAVE10", promotionId, totalUses, null, null, null, Now).Value;

    [Fact]
    public void No_Promotions_Produces_Zero_Discounts()
    {
        var result = PricingEngine.Evaluate(Context(), [], Now);

        Assert.Empty(result.ItemDiscounts);
        Assert.Equal(0m, result.CartDiscount);
        Assert.Equal(0m, result.ShippingDiscount);
        Assert.Empty(result.AppliedRuleIds);
    }

    [Fact]
    public void Order_Percent_Discount_Applies_To_Cart()
    {
        var promotion = PromotionOf("10% Off", DiscountType.Order, DiscountBasis.Percent, 10m);

        var result = PricingEngine.Evaluate(Context(), [promotion], Now);

        Assert.Equal(10.00m, result.CartDiscount);
        Assert.Equal([promotion.Id], result.AppliedRuleIds);
        Assert.Equal(100.00m, result.Subtotal);
    }

    [Fact]
    public void Order_Amount_Discount_Is_Capped()
    {
        var promotion = PromotionOf("Flat 50", DiscountType.Order, DiscountBasis.Amount, 50m, cap: 20m);

        var result = PricingEngine.Evaluate(Context(), [promotion], Now);

        Assert.Equal(20.00m, result.CartDiscount);
    }

    [Fact]
    public void Amount_Discount_Above_Subtotal_Is_Clamped_To_Subtotal()
    {
        var promotion = PromotionOf("Flat 999", DiscountType.Order, DiscountBasis.Amount, 999m);

        var result = PricingEngine.Evaluate(Context(), [promotion], Now);

        Assert.Equal(100.00m, result.CartDiscount);
        Assert.Equal(0m, Math.Max(0m, result.Subtotal - result.CartDiscount));
    }

    [Fact]
    public void Item_Discount_Applies_Only_To_Target_Product()
    {
        var promotion = PromotionOf(
            "Product Offer",
            DiscountType.Product,
            DiscountBasis.Percent,
            20m,
            conditions: [new ProductCondition([ProductA])]);

        var context = Context([Line(ProductA, 100.00m, 1), Line(ProductB, 50.00m, 2)]);

        var result = PricingEngine.Evaluate(context, [promotion], Now);

        Assert.Single(result.ItemDiscounts);
        Assert.Equal(ProductA, result.ItemDiscounts[0].ProductId);
        Assert.Equal(20.00m, result.ItemDiscounts[0].Amount);
    }

    [Fact]
    public void Overlapping_Best_Of_Promotions_Pick_The_Larger()
    {
        var tenPercent = PromotionOf("Ten", DiscountType.Order, DiscountBasis.Percent, 10m);
        var twentyPercent = PromotionOf("Twenty", DiscountType.Order, DiscountBasis.Percent, 20m);

        var result = PricingEngine.Evaluate(Context(), [tenPercent, twentyPercent], Now);

        Assert.Equal(20.00m, result.CartDiscount);
        Assert.Single(result.AppliedRuleIds);
        Assert.Equal(twentyPercent.Id, result.AppliedRuleIds[0]);
    }

    [Fact]
    public void Stacking_Promotions_Are_Additive()
    {
        var first = PromotionOf("Stack A", DiscountType.Order, DiscountBasis.Percent, 10m, stacking: new StackingMatrix(true, []));
        var second = PromotionOf("Stack B", DiscountType.Order, DiscountBasis.Percent, 10m, stacking: new StackingMatrix(true, []));

        var result = PricingEngine.Evaluate(Context(), [first, second], Now);

        Assert.Equal(20.00m, result.CartDiscount);
        Assert.Equal(2, result.AppliedRuleIds.Count);
    }

    [Fact]
    public void Mixed_Stacking_Policy_Falls_Back_To_Best_Of()
    {
        var stackable = PromotionOf("Stack A", DiscountType.Order, DiscountBasis.Percent, 10m, stacking: new StackingMatrix(true, []));
        var exclusive = PromotionOf("Exclusive", DiscountType.Order, DiscountBasis.Amount, 30m);

        var result = PricingEngine.Evaluate(Context(), [stackable, exclusive], Now);

        Assert.Equal(30.00m, result.CartDiscount);
        Assert.Single(result.AppliedRuleIds);
        Assert.Equal(exclusive.Id, result.AppliedRuleIds[0]);
    }

    [Fact]
    public void Shipping_Percent_100_Means_Free_Shipping()
    {
        var promotion = PromotionOf("Free Ship", DiscountType.Shipping, DiscountBasis.Percent, 100m);

        var result = PricingEngine.Evaluate(Context(), [promotion], Now);

        Assert.Equal(9.90m, result.ShippingDiscount);
    }

    [Fact]
    public void Shipping_Discount_Is_Clamped_To_Rate()
    {
        var promotion = PromotionOf("Ship Off", DiscountType.Shipping, DiscountBasis.Amount, 50m);

        var result = PricingEngine.Evaluate(Context(shippingRate: 4.90m), [promotion], Now);

        Assert.Equal(4.90m, result.ShippingDiscount);
    }

    [Fact]
    public void Priority_Order_Applies_Item_Cart_Then_Shipping()
    {
        var itemPromo = PromotionOf("Item", DiscountType.Product, DiscountBasis.Percent, 10m);
        var orderPromo = PromotionOf("Order", DiscountType.Order, DiscountBasis.Percent, 10m);
        var shipPromo = PromotionOf("Ship", DiscountType.Shipping, DiscountBasis.Percent, 50m);

        var result = PricingEngine.Evaluate(Context(), [orderPromo, shipPromo, itemPromo], Now);

        Assert.Single(result.ItemDiscounts);
        Assert.Equal(10.00m, result.ItemDiscounts[0].Amount);
        Assert.Equal(10.00m, result.CartDiscount);
        Assert.Equal(4.95m, result.ShippingDiscount);
        Assert.Equal(3, result.AppliedRuleIds.Count);
    }

    [Fact]
    public void Coupon_Stacks_On_Top_Of_Auto_Promotion()
    {
        var autoPromo = PromotionOf("Auto 10", DiscountType.Order, DiscountBasis.Percent, 10m);
        var couponPromo = PromotionOf("Coupon 20", DiscountType.Order, DiscountBasis.Percent, 20m);
        var coupon = CouponOf(couponPromo.Id);

        var result = PricingEngine.Evaluate(Context(), [autoPromo, couponPromo], Now, coupon);

        Assert.Equal(30.00m, result.CartDiscount);
        Assert.Contains(couponPromo.Id, result.AppliedRuleIds);
    }

    [Fact]
    public void Inactive_Coupon_Is_Not_Applied()
    {
        var couponPromo = PromotionOf("Coupon 20", DiscountType.Order, DiscountBasis.Percent, 20m);
        var expired = Coupon.Create("OLD", couponPromo.Id, 5, null, Now.AddDays(-2), Now.AddDays(-1), Now).Value;

        var result = PricingEngine.Evaluate(Context(), [couponPromo], Now, expired);

        Assert.Equal(0m, result.CartDiscount);
    }

    [Fact]
    public void Coupon_With_Uneligible_Promotion_Is_Not_Applied()
    {
        var couponPromo = PromotionOf("Other Country", DiscountType.Order, DiscountBasis.Percent, 20m, countries: ["US"]);
        var coupon = CouponOf(couponPromo.Id);

        var result = PricingEngine.Evaluate(Context(), [couponPromo], Now, coupon);

        Assert.Equal(0m, result.CartDiscount);
    }

    [Fact]
    public void Cart_Discount_Never_Exceeds_Remaining_After_Item_Discounts()
    {
        var itemPromo = PromotionOf("Item 50", DiscountType.Product, DiscountBasis.Percent, 50m);
        var orderPromo = PromotionOf("Order 80", DiscountType.Order, DiscountBasis.Percent, 80m);

        var context = Context([Line(ProductA, 100.00m, 1)]);
        var result = PricingEngine.Evaluate(context, [itemPromo, orderPromo], Now);

        var itemTotal = result.ItemDiscounts.Sum(d => d.Amount);
        Assert.True(result.CartDiscount <= Math.Max(0m, result.Subtotal - itemTotal));
        Assert.True(result.Subtotal - itemTotal - result.CartDiscount >= 0m);
    }

    [Fact]
    public void Applied_Rule_Ids_Are_Deterministic()
    {
        var a = PromotionOf("A", DiscountType.Order, DiscountBasis.Percent, 10m);
        var b = PromotionOf("B", DiscountType.Order, DiscountBasis.Percent, 20m, stacking: new StackingMatrix(true, []));

        var result = PricingEngine.Evaluate(Context(), [a, b], Now);

        Assert.Equal(result.AppliedRuleIds.OrderBy(id => id), result.AppliedRuleIds);
    }
}

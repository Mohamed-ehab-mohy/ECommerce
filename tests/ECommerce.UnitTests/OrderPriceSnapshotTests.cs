using ECommerce.Domain.Orders;
using ECommerce.Domain.Pricing;

namespace ECommerce.UnitTests;

public sealed class OrderPriceSnapshotTests
{
    private static readonly DateTime Now = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

    private static Order PlaceOrder(TotalsSnapshot totals)
    {
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 100.00m, 90.00m, 1, null)],
            totals);

        var address = new AddressSnapshot("Test User", null, "1 Test St", "Dubai", null, "AE", "00000");

        return Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test@example.com",
            "AED",
            "ORD-TEST-000001",
            snapshot,
            address,
            address,
            "SM-1",
            Guid.NewGuid(),
            Now,
            null,
            [Guid.NewGuid()]);
    }

    [Fact]
    public void Order_Totals_Are_Immutable_After_Promotion_Changes()
    {
        var promotion = Promotion.Create(
            "Summer Sale",
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 10m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            null,
            null,
            Now).Value;
        promotion.Activate(Now);

        var order = PlaceOrder(new TotalsSnapshot(100.00m, 0m, 10.00m, 9.90m, 5.00m, 104.90m));
        var grandTotalBefore = order.GrandTotal;

        promotion.Update(
            "Summer Sale",
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Percent, 90m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            Now);
        promotion.Pause(Now);

        Assert.Equal(grandTotalBefore, order.GrandTotal);
        Assert.Equal(104.90m, order.GrandTotal);
        Assert.Equal(10.00m, order.CartDiscount);
    }
}

using ECommerce.Domain.Events;
using ECommerce.Domain.Orders;

namespace ECommerce.UnitTests;

public sealed class OrderTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m));

    private static Order CreateOrder() =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "ahmed@example.com",
            "USD",
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            Now);

    [Fact]
    public void Create_Sets_Placed_Status_Totals_And_Items()
    {
        var order = CreateOrder();

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.NotNull(order.PlacedAt);
        Assert.Equal(30.00m, order.Subtotal);
        Assert.Equal(10.00m, order.ItemDiscount);
        Assert.Equal(39.90m, order.GrandTotal);
        Assert.Equal("USD", order.Currency);
        Assert.Equal("ahmed@example.com", order.CustomerEmail);

        var item = Assert.Single(order.Items);
        Assert.Equal(order.Id, item.OrderId);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(15.00m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void Create_Raises_OrderPlaced_Event()
    {
        var order = CreateOrder();

        var placed = Assert.Single(order.DomainEvents.OfType<OrderPlaced>());
        Assert.Equal(order.Id, placed.OrderId);
        Assert.Equal(order.CartId, placed.CartId);
        Assert.Equal(39.90m, placed.Total);
    }

    [Fact]
    public void Cancel_Transitions_To_Cancelled()
    {
        var order = CreateOrder();

        var result = order.Cancel(Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_From_Non_Placed_Is_Rejected()
    {
        var order = CreateOrder();
        order.Cancel(Now);

        var result = order.Cancel(Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidState, result.Error);
    }
}

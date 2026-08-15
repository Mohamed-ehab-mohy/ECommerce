using ECommerce.Domain.Events;
using ECommerce.Domain.Orders;

namespace ECommerce.UnitTests;

public sealed class OrderRealtimeEventTests
{
    private static readonly DateTime Now = new(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

    private static readonly Guid CustomerId = Guid.NewGuid();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0m));

    private static Order CreateOrder() =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CustomerId,
            "ahmed@example.com",
            "USD",
            "E-20260815-000001",
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            Now);

    [Fact]
    public void Create_Raises_OrderStatusChanged_To_Placed_And_OrderTimelineUpdated()
    {
        var order = CreateOrder();

        var statusChanged = Assert.Single(order.DomainEvents.OfType<OrderStatusChanged>());
        Assert.Equal(order.Id, statusChanged.OrderId);
        Assert.Equal(CustomerId, statusChanged.CustomerId);
        Assert.Equal(OrderStatus.Placed, statusChanged.From);
        Assert.Equal(OrderStatus.Placed, statusChanged.To);

        Assert.Contains(order.DomainEvents, domainEvent =>
            domainEvent is OrderTimelineUpdated timeline && timeline.OrderId == order.Id);
    }

    [Fact]
    public void Cancel_Raises_OrderStatusChanged_With_From_And_To()
    {
        var order = CreateOrder();

        order.Cancel("customer-request", "customer", null, "trace-1", Now.AddMinutes(1));

        var statusChanged = order.DomainEvents.OfType<OrderStatusChanged>().Last();
        Assert.Equal(OrderStatus.Placed, statusChanged.From);
        Assert.Equal(OrderStatus.Cancelled, statusChanged.To);
        Assert.Equal(order.OrderNumber, statusChanged.OrderNumber);
    }

    [Fact]
    public void UpdateShippingAddress_Raises_OrderTimelineUpdated_Without_Status_Change()
    {
        var order = CreateOrder();
        var statusChangesBefore = order.DomainEvents.OfType<OrderStatusChanged>().Count();
        var timelineBefore = order.DomainEvents.OfType<OrderTimelineUpdated>().Count();

        var result = order.UpdateShippingAddress(
            new AddressSnapshot("Ahmed Hassan", "0501234567", "2 Downtown Rd", "Dubai", "Dubai", "AE", "00000"),
            "customer",
            CustomerId,
            "trace-1",
            Now.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(statusChangesBefore, order.DomainEvents.OfType<OrderStatusChanged>().Count());
        Assert.Equal(timelineBefore + 1, order.DomainEvents.OfType<OrderTimelineUpdated>().Count());
    }
}

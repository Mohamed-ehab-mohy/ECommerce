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
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0m));

    private static Order CreateOrder() =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "ahmed@example.com",
            "USD",
            "E-20260807-000001",
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
        Assert.Equal("E-20260807-000001", order.OrderNumber);
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
    public void Create_Records_Initial_Timeline_Entry()
    {
        var order = CreateOrder();

        var entry = Assert.Single(order.StatusLogs);
        Assert.Null(entry.FromStatus);
        Assert.Equal(OrderStatus.Placed, entry.ToStatus);
        Assert.Equal("system", entry.ActorType);
        Assert.Equal(Now, entry.OccurredAt);
    }

    [Fact]
    public void Create_Raises_OrderPlaced_Event()
    {
        var order = CreateOrder();

        var placed = Assert.Single(order.DomainEvents.OfType<OrderPlaced>());
        Assert.Equal(order.Id, placed.OrderId);
        Assert.Equal("E-20260807-000001", placed.OrderNumber);
        Assert.Equal(order.CartId, placed.CartId);
        Assert.Equal(39.90m, placed.Total);
    }

    [Fact]
    public void Cancel_Transitions_To_Cancelled_And_Logs_Timeline()
    {
        var order = CreateOrder();

        var result = order.Cancel("customer-request", "customer", null, "trace-1", Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);

        Assert.Equal(2, order.StatusLogs.Count);
        var entry = order.StatusLogs.Last();
        Assert.Equal(OrderStatus.Placed, entry.FromStatus);
        Assert.Equal(OrderStatus.Cancelled, entry.ToStatus);
        Assert.Equal("customer", entry.ActorType);
        Assert.Equal("trace-1", entry.TraceId);

        var cancelled = Assert.Single(order.DomainEvents.OfType<OrderCancelled>());
        Assert.Equal(order.Id, cancelled.OrderId);
        Assert.Equal("E-20260807-000001", cancelled.OrderNumber);
        Assert.Equal("customer-request", cancelled.Reason);
    }

    [Fact]
    public void Cancel_From_Non_Placed_Is_Rejected()
    {
        var order = CreateOrder();
        order.Cancel("customer-request", "customer", null, null, Now);

        var result = order.Cancel("customer-request", "customer", null, null, Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CancellationNotAllowed, result.Error);
    }

    [Fact]
    public void MarkBackordered_Transitions_To_Backordered_And_Adds_Items()
    {
        var order = CreateOrder();
        var productId = order.Items.Single().ProductId;

        var result = order.MarkBackordered([(productId, "SKU-1", 2)], Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Backordered, order.Status);

        var item = Assert.Single(order.BackorderItems);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(0, item.FilledQuantity);
        Assert.Equal(BackorderStatus.Open, item.Status);

        var entry = order.StatusLogs.Last();
        Assert.Equal(OrderStatus.Placed, entry.FromStatus);
        Assert.Equal(OrderStatus.Backordered, entry.ToStatus);

        var evt = Assert.Single(order.DomainEvents.OfType<OrderBackordered>());
        Assert.Equal(order.Id, evt.OrderId);
        var line = Assert.Single(evt.Lines);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public void MarkBackordered_From_Non_Placed_Is_Rejected()
    {
        var order = CreateOrder();
        order.Cancel("customer-request", "customer", null, null, Now);

        var result = order.MarkBackordered([(order.Items.Single().ProductId, "SKU-1", 2)], Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidState, result.Error);
    }

    [Fact]
    public void MarkBackordered_With_Duplicate_Open_Line_Is_Rejected()
    {
        var order = CreateOrder();
        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 2)], Now.AddMinutes(1));

        var result = order.MarkBackordered([(productId, "SKU-1", 1)], Now.AddMinutes(2));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.BackorderAlreadyOpen, result.Error);
    }

    [Fact]
    public void FillBackorderItems_Fills_FIFO_And_Completes_When_All_Filled()
    {
        var order = CreateOrder();
        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 5)], Now.AddMinutes(1));

        var first = order.FillBackorderItems("SKU-1", 2, Now.AddMinutes(2));
        Assert.Equal(2, first);
        Assert.Equal(OrderStatus.Backordered, order.Status);
        Assert.Equal(2, order.BackorderItems.Single().FilledQuantity);

        var second = order.FillBackorderItems("SKU-1", 3, Now.AddMinutes(3));
        Assert.Equal(3, second);
        Assert.Equal(OrderStatus.AwaitingFulfillment, order.Status);
        Assert.Equal(BackorderStatus.Filled, order.BackorderItems.Single().Status);
        Assert.NotNull(order.BackorderItems.Single().FilledAt);

        var entry = order.StatusLogs.Last();
        Assert.Equal(OrderStatus.Backordered, entry.FromStatus);
        Assert.Equal(OrderStatus.AwaitingFulfillment, entry.ToStatus);

        Assert.Equal(2, order.DomainEvents.OfType<BackorderFilled>().Count());
        Assert.Contains(order.DomainEvents.OfType<BackorderFilled>(), evt => evt.Quantity == 2);
        Assert.Contains(order.DomainEvents.OfType<BackorderFilled>(), evt => evt.Quantity == 3);
    }

    [Fact]
    public void FillBackorderItems_Caps_At_Remaining_Quantity()
    {
        var order = CreateOrder();
        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 2)], Now.AddMinutes(1));

        var filled = order.FillBackorderItems("SKU-1", 10, Now.AddMinutes(2));

        Assert.Equal(2, filled);
        Assert.Equal(2, order.BackorderItems.Single().FilledQuantity);
        Assert.Equal(OrderStatus.AwaitingFulfillment, order.Status);
    }

    [Fact]
    public void FillBackorderItems_With_Unknown_Sku_Returns_Zero()
    {
        var order = CreateOrder();
        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 2)], Now.AddMinutes(1));

        var filled = order.FillBackorderItems("SKU-9", 2, Now.AddMinutes(2));

        Assert.Equal(0, filled);
        Assert.Equal(OrderStatus.Backordered, order.Status);
        Assert.Empty(order.DomainEvents.OfType<BackorderFilled>());
    }

    [Fact]
    public void UpdateShippingAddress_Changes_Address_And_Raises_Event()
    {
        var order = CreateOrder();
        var corrected = new AddressSnapshot(
            "Mona Ali", "0507654321", "2 Marina Walk", "Abu Dhabi", null, "AE", "00001");

        var result = order.UpdateShippingAddress(corrected, "user", null, "trace-1", Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(corrected, order.ShippingAddress);
        Assert.NotEqual(Address, order.ShippingAddress);

        var evt = Assert.Single(order.DomainEvents.OfType<OrderShippingAddressUpdated>());
        Assert.Equal(order.Id, evt.OrderId);
        Assert.Equal(Address, evt.PreviousAddress);
        Assert.Equal(corrected, evt.NewAddress);
    }

    [Fact]
    public void UpdateShippingAddress_After_Shipping_Is_Rejected()
    {
        var order = CreateOrder();
        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 2)], Now.AddMinutes(1));
        order.FillBackorderItems("SKU-1", 2, Now.AddMinutes(2));
        order.StartFulfillment("user", null, null, Now.AddMinutes(3));
        order.MarkPacked("user", null, null, Now.AddMinutes(4));
        order.Ship("dhl", ["TRK-1"], "user", null, null, Now.AddMinutes(5));

        var result = order.UpdateShippingAddress(
            new AddressSnapshot("Mona Ali", null, "2 Marina Walk", "Abu Dhabi", null, "AE", "00001"),
            "user",
            null,
            null,
            Now.AddMinutes(6));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.AddressCorrectionNotAllowed, result.Error);
        Assert.Equal(Address, order.ShippingAddress);
    }

    [Fact]
    public void UpdateShippingAddress_With_Identical_Address_Is_Noop()
    {
        var order = CreateOrder();

        var result = order.UpdateShippingAddress(Address, "user", null, null, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Empty(order.DomainEvents.OfType<OrderShippingAddressUpdated>());
    }
}

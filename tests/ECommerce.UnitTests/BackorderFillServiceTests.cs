using ECommerce.Domain.Events;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Services;

namespace ECommerce.UnitTests;

public sealed class BackorderFillServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m));

    private static Order CreateOrder(DateTime createdAt, string orderNumber)
    {
        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "ahmed@example.com",
            "USD",
            orderNumber,
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            createdAt);

        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 3)], createdAt.AddMinutes(1));
        return order;
    }

    private static BackorderFillService CreateService(
        FakeOrderRepository orders,
        FakeStockAllocator allocator,
        FakeUnitOfWork unitOfWork) =>
        new(orders, allocator, unitOfWork, new FixedTimeProvider(UtcNow));

    [Fact]
    public async Task Fill_With_No_Open_Backorders_Does_Not_Allocate()
    {
        var orders = new FakeOrderRepository();
        var allocator = new FakeStockAllocator();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(orders, allocator, unitOfWork);

        await service.FillForSkuAsync("SKU-1", CancellationToken.None);

        Assert.Equal(0, allocator.AllocateCount);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Fill_Allocates_And_Fills_Fifo_Across_Orders()
    {
        var orders = new FakeOrderRepository();
        var orderA = CreateOrder(UtcNow, "E-20260813-000001");
        var orderB = CreateOrder(UtcNow.AddMinutes(1), "E-20260813-000002");
        orders.Add(orderA);
        orders.Add(orderB);

        var allocator = new FakeStockAllocator(
            lines: [new StockAllocationLine(Guid.NewGuid(), "SKU-1", Guid.NewGuid(), 6)]);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(orders, allocator, unitOfWork);

        await service.FillForSkuAsync("SKU-1", CancellationToken.None);

        Assert.Equal(1, allocator.AllocateCount);
        Assert.Equal("BACKORDER", allocator.LastReason);
        Assert.Equal("BACKORDER:SKU-1", allocator.LastReference);

        var allocationRequest = Assert.Single(allocator.LastItems);
        Assert.Equal("SKU-1", allocationRequest.Sku);
        Assert.Equal(6, allocationRequest.Quantity);

        Assert.Equal(3, orderA.BackorderItems.Single().FilledQuantity);
        Assert.Equal(BackorderStatus.Filled, orderA.BackorderItems.Single().Status);
        Assert.Equal(OrderStatus.AwaitingFulfillment, orderA.Status);

        Assert.Equal(3, orderB.BackorderItems.Single().FilledQuantity);
        Assert.Equal(BackorderStatus.Filled, orderB.BackorderItems.Single().Status);
        Assert.Equal(OrderStatus.AwaitingFulfillment, orderB.Status);

        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Contains(orderA.DomainEvents.OfType<BackorderFilled>(), evt => evt.Quantity == 3);
        Assert.Contains(orderB.DomainEvents.OfType<BackorderFilled>(), evt => evt.Quantity == 3);
    }

    [Fact]
    public async Task Fill_Partial_Allocation_Fills_Fifo_And_Leaves_Order_Backordered()
    {
        var orders = new FakeOrderRepository();
        var orderA = CreateOrder(UtcNow, "E-20260813-000001");
        var orderB = CreateOrder(UtcNow.AddMinutes(1), "E-20260813-000002");
        orders.Add(orderA);
        orders.Add(orderB);

        var allocator = new FakeStockAllocator(
            lines: [new StockAllocationLine(Guid.NewGuid(), "SKU-1", Guid.NewGuid(), 2)]);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(orders, allocator, unitOfWork);

        await service.FillForSkuAsync("SKU-1", CancellationToken.None);

        Assert.Equal(2, orderA.BackorderItems.Single().FilledQuantity);
        Assert.Equal(OrderStatus.Backordered, orderA.Status);

        Assert.Equal(0, orderB.BackorderItems.Single().FilledQuantity);
        Assert.Equal(OrderStatus.Backordered, orderB.Status);

        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Fill_With_No_Allocated_Quantity_Does_Not_Save()
    {
        var orders = new FakeOrderRepository();
        var orderA = CreateOrder(UtcNow, "E-20260813-000001");
        orders.Add(orderA);

        var allocator = new FakeStockAllocator(
            shortfalls: [new StockShortfall("SKU-1", 3, 0)]);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(orders, allocator, unitOfWork);

        await service.FillForSkuAsync("SKU-1", CancellationToken.None);

        Assert.Equal(0, orderA.BackorderItems.Single().FilledQuantity);
        Assert.Equal(OrderStatus.Backordered, orderA.Status);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

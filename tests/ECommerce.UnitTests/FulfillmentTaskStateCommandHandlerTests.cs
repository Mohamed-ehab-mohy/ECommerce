using ECommerce.Domain.Catalog;
using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;

namespace ECommerce.UnitTests;

public sealed class FulfillmentTaskStateCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static readonly Guid PickerId = Guid.NewGuid();

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeFulfillmentTaskRepository _tasks = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private AssignFulfillmentTaskCommandHandler AssignHandler =>
        new(_tasks, _unitOfWork, new FixedTimeProvider(UtcNow), new AssignFulfillmentTaskCommandValidator());

    private StartPickingFulfillmentTaskCommandHandler StartPickingHandler =>
        new(_tasks, _orders, _unitOfWork, new FixedTimeProvider(UtcNow), new StartPickingFulfillmentTaskCommandValidator());

    private MarkFulfillmentTaskPackedCommandHandler PackHandler =>
        new(_tasks, _orders, _unitOfWork, new FixedTimeProvider(UtcNow), new MarkFulfillmentTaskPackedCommandValidator());

    private (FulfillmentTask Task, Order Order) SeedQueuedTask()
    {
        var product = Product.Create(
            "SKU-1", "widget", "en", "Widget", null,
            "USD", 20.00m, 15.00m, null, null, false, ProductStatus.Active, UtcNow);

        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 2, null)],
            new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m));

        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, "ahmed@example.com", "USD",
            "E-20260813-0001", snapshot,
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            "standard", Guid.NewGuid(), UtcNow);
        order.MarkBackordered([(product.Id, product.Sku, 2)], UtcNow);
        order.FillBackorderItems(product.Sku, 2, UtcNow);

        var task = FulfillmentTask.Create(order.Id, WarehouseId, 1, UtcNow, "A");
        task.AddItem(product.Id, product.Sku, 2, null);

        _orders.Add(order);
        _tasks.Add(task);
        return (task, order);
    }

    [Fact]
    public async Task Assign_Sets_Assignee()
    {
        var (task, _) = SeedQueuedTask();

        var result = await AssignHandler.Handle(
            new AssignFulfillmentTaskCommand(task.Id, PickerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(FulfillmentTaskStatus.Assigned, task.Status);
        Assert.Equal(PickerId, task.AssignedTo);
        Assert.Equal("Assigned", result.Value.Status);
    }

    [Fact]
    public async Task Assign_Unknown_Task_Returns_NotFound()
    {
        var result = await AssignHandler.Handle(
            new AssignFulfillmentTaskCommand(Guid.NewGuid(), PickerId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.TaskNotFound, result.Error);
    }

    [Fact]
    public async Task StartPicking_Transitions_Task_And_Order()
    {
        var (task, order) = SeedQueuedTask();
        task.Assign(PickerId, UtcNow);

        var result = await StartPickingHandler.Handle(
            new StartPickingFulfillmentTaskCommand(task.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(FulfillmentTaskStatus.Picking, task.Status);
        Assert.Equal(OrderStatus.Picking, order.Status);
    }

    [Fact]
    public async Task Pack_Transitions_Task_And_Order()
    {
        var (task, order) = SeedQueuedTask();
        task.Assign(PickerId, UtcNow);

        var started = await StartPickingHandler.Handle(
            new StartPickingFulfillmentTaskCommand(task.Id),
            CancellationToken.None);
        Assert.True(started.IsSuccess, started.Error.Description);

        var result = await PackHandler.Handle(
            new MarkFulfillmentTaskPackedCommand(task.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(FulfillmentTaskStatus.Packed, task.Status);
        Assert.Equal(OrderStatus.Packed, order.Status);
    }

    [Fact]
    public async Task Pack_Before_Picking_Returns_NotPicking()
    {
        var (task, _) = SeedQueuedTask();

        var result = await PackHandler.Handle(
            new MarkFulfillmentTaskPackedCommand(task.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.NotPicking, result.Error);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

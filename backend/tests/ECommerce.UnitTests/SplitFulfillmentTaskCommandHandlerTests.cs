using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Inventory;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;

namespace ECommerce.UnitTests;

public sealed class SplitFulfillmentTaskCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 18, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeFulfillmentTaskRepository _tasks = new();

    private readonly FakeWarehouseRepository _warehouses = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private Guid OtherWarehouseId { get; set; }

    private SplitFulfillmentTaskCommandHandler Handler =>
        new(
            _tasks,
            _warehouses,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new SplitFulfillmentTaskCommandValidator());

    private FulfillmentTask SeedQueuedTask()
    {
        _warehouses.Add(Warehouse.Create("W-1", "WH1", "Dubai", "UTC", WarehouseStatus.Active, UtcNow));

        var targetWarehouse = Warehouse.Create("W-2", "WH2", "Abu Dhabi", "UTC", WarehouseStatus.Active, UtcNow);
        OtherWarehouseId = targetWarehouse.Id;
        _warehouses.Add(targetWarehouse);

        var task = FulfillmentTask.Create(Guid.NewGuid(), WarehouseId, 1, UtcNow, "A");
        task.AddItem(Guid.NewGuid(), "SKU-1", 2, "A-01");
        task.AddItem(Guid.NewGuid(), "SKU-2", 1, "A-02");
        _tasks.Add(task);
        return task;
    }

    private SplitFulfillmentTaskCommand CreateCommand(Guid taskId, IReadOnlyList<Guid> itemIds) =>
        new(taskId, OtherWarehouseId, itemIds, 3, "B");

    [Fact]
    public async Task Split_Creates_New_Task_And_Removes_Items_From_Parent()
    {
        var task = SeedQueuedTask();
        var movingId = task.Items.First(item => item.Sku == "SKU-2").Id;

        var result = await Handler.Handle(CreateCommand(task.Id, [movingId]), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var part = Assert.Single(_tasks.Tasks, candidate => candidate.Id != task.Id);
        Assert.Equal(OtherWarehouseId, part.WarehouseId);
        Assert.Equal(task.Id, part.ParentTaskId);
        Assert.Equal("B", result.Value.Zone);
        Assert.Single(task.Items);
        Assert.Single(part.Items);
    }

    [Fact]
    public async Task Split_Unknown_Task_Returns_NotFound()
    {
        var result = await Handler.Handle(
            new SplitFulfillmentTaskCommand(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], 3, "B"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.TaskNotFound, result.Error);
    }

    [Fact]
    public async Task Split_Unknown_Warehouse_Returns_WarehouseNotFound()
    {
        var task = SeedQueuedTask();
        var movingId = task.Items.First().Id;

        var result = await Handler.Handle(
            new SplitFulfillmentTaskCommand(task.Id, Guid.NewGuid(), [movingId], 3, "B"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.WarehouseNotFound, result.Error);
    }

    [Fact]
    public async Task Split_All_Items_Returns_InvalidSplit()
    {
        var task = SeedQueuedTask();
        var allIds = task.Items.Select(item => item.Id).ToList();

        var result = await Handler.Handle(CreateCommand(task.Id, allIds), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.InvalidSplit, result.Error);
    }

    [Fact]
    public async Task Split_Empty_Item_List_Returns_Validation_Failure()
    {
        var task = SeedQueuedTask();

        var result = await Handler.Handle(
            new SplitFulfillmentTaskCommand(task.Id, OtherWarehouseId, [], 3, "B"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

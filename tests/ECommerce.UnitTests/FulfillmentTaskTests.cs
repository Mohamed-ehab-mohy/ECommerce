using ECommerce.Domain.Events;
using ECommerce.Domain.Fulfillment;

namespace ECommerce.UnitTests;

public sealed class FulfillmentTaskTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private static readonly Guid OrderId = Guid.NewGuid();

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static FulfillmentTask CreateTask() => FulfillmentTask.Create(OrderId, WarehouseId, 5, UtcNow, "A");

    private static FulfillmentTask CreateTaskWithItem()
    {
        var task = CreateTask();
        task.AddItem(Guid.NewGuid(), "SKU-1", 2, "A-01");
        return task;
    }

    [Fact]
    public void Create_Sets_Queued_Status_And_Defaults()
    {
        var task = CreateTask();

        Assert.Equal(OrderId, task.OrderId);
        Assert.Equal(WarehouseId, task.WarehouseId);
        Assert.Equal("A", task.Zone);
        Assert.Equal(5, task.Priority);
        Assert.Equal(FulfillmentTaskStatus.Queued, task.Status);
        Assert.Null(task.AssignedTo);
        Assert.Empty(task.Items);
    }

    [Fact]
    public void AddItem_Adds_Line_With_Quantity()
    {
        var task = CreateTask();
        task.AddItem(Guid.NewGuid(), "SKU-1", 3, "B-02");

        var item = Assert.Single(task.Items);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(3, item.Quantity);
        Assert.Equal("B-02", item.BinLocation);
        Assert.Equal(task.Id, item.TaskId);
    }

    [Fact]
    public void Assign_Moves_To_Assigned_And_Raises_Event()
    {
        var task = CreateTask();
        var picker = Guid.NewGuid();

        var result = task.Assign(picker, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(FulfillmentTaskStatus.Assigned, task.Status);
        Assert.Equal(picker, task.AssignedTo);
        Assert.Equal(UtcNow, task.AssignedAt);
        Assert.Contains(task.DomainEvents, domainEvent => domainEvent is FulfillmentTaskAssigned assigned && assigned.PickerId == picker);
    }

    [Fact]
    public void Assign_NonQueued_Returns_NotQueued()
    {
        var task = CreateTask();
        task.Assign(Guid.NewGuid(), UtcNow);

        var result = task.Assign(Guid.NewGuid(), UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.NotQueued, result.Error);
    }

    [Fact]
    public void StartPicking_Requires_Assigned()
    {
        var task = CreateTask();

        var result = task.StartPicking(UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.NotAssigned, result.Error);
    }

    [Fact]
    public void StartPicking_Moves_To_Picking()
    {
        var task = CreateTask();
        task.Assign(Guid.NewGuid(), UtcNow);

        var result = task.StartPicking(UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(FulfillmentTaskStatus.Picking, task.Status);
        Assert.Equal(UtcNow, task.StartedAt);
    }

    [Fact]
    public void MarkPacked_Requires_Picking()
    {
        var task = CreateTask();
        task.Assign(Guid.NewGuid(), UtcNow);

        var result = task.MarkPacked(UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.NotPicking, result.Error);
    }

    [Fact]
    public void MarkPacked_Moves_To_Packed()
    {
        var task = CreateTask();
        task.Assign(Guid.NewGuid(), UtcNow);
        task.StartPicking(UtcNow);

        var result = task.MarkPacked(UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(FulfillmentTaskStatus.Packed, task.Status);
        Assert.Equal(UtcNow, task.PackedAt);
    }

    [Fact]
    public void MarkShipped_Requires_Packed()
    {
        var task = CreateTaskWithItem();

        var result = task.MarkShipped(UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.NotPacked, result.Error);
    }

    [Fact]
    public void MarkShipped_Moves_To_Shipped_And_Raises_Event()
    {
        var task = CreateTaskWithItem();
        task.Assign(Guid.NewGuid(), UtcNow);
        task.StartPicking(UtcNow);
        task.MarkPacked(UtcNow);

        var result = task.MarkShipped(UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(FulfillmentTaskStatus.Shipped, task.Status);
        Assert.Equal(UtcNow, task.ShippedAt);
        Assert.Contains(task.DomainEvents, domainEvent => domainEvent is FulfillmentTaskShipped);
    }

    [Fact]
    public void Cancel_From_Queued_Moves_To_Cancelled()
    {
        var task = CreateTask();

        var result = task.Cancel("duplicate", UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(FulfillmentTaskStatus.Cancelled, task.Status);
        Assert.Equal("duplicate", task.CancellationReason);
        Assert.Contains(task.DomainEvents, domainEvent => domainEvent is FulfillmentTaskCancelled);
    }

    [Fact]
    public void Cancel_Shipped_Returns_InvalidState()
    {
        var task = CreateTaskWithItem();
        task.Assign(Guid.NewGuid(), UtcNow);
        task.StartPicking(UtcNow);
        task.MarkPacked(UtcNow);
        task.MarkShipped(UtcNow);

        var result = task.Cancel("late", UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.InvalidState, result.Error);
    }
}

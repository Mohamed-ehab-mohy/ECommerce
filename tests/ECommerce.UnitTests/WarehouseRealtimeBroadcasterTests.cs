using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Events;
using ECommerce.Domain.Fulfillment;
using ECommerce.Infrastructure.Realtime;

namespace ECommerce.UnitTests;

public sealed class WarehouseRealtimeBroadcasterTests
{
    private static readonly DateTime OccurredAt = new(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static (WarehouseRealtimeBroadcaster Broadcaster, FakeRealtimeHubContext Hub) Create()
    {
        var hub = new FakeRealtimeHubContext();
        var broadcaster = new WarehouseRealtimeBroadcaster(new RealtimeEventForwarder(new FakeRealtimeEventStore()), hub);
        return (broadcaster, hub);
    }

    [Fact]
    public async Task FulfillmentTaskCreated_Pushes_NewFulfillmentTask_To_Warehouse_Group()
    {
        var (broadcaster, hub) = Create();
        var taskId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await broadcaster.HandleAsync(
            new FulfillmentTaskCreated(taskId, orderId, WarehouseId, "A", 5)
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal($"wh:{WarehouseId}", groupKey);
        Assert.Equal(RealtimeEventTypes.NewFulfillmentTask, envelope.Type);
        var data = Assert.IsType<NewFulfillmentTaskData>(envelope.Data);
        Assert.Equal(taskId, data.TaskId);
        Assert.Equal(orderId, data.OrderId);
        Assert.Equal("A", data.Zone);
        Assert.Equal(5, data.Priority);
    }

    [Theory]
    [InlineData(FulfillmentTaskStatus.Assigned, "Assigned")]
    [InlineData(FulfillmentTaskStatus.Picking, "Picking")]
    [InlineData(FulfillmentTaskStatus.Packed, "Packed")]
    [InlineData(FulfillmentTaskStatus.Shipped, "Shipped")]
    [InlineData(FulfillmentTaskStatus.Cancelled, "Cancelled")]
    public async Task Task_Transitions_Push_TaskStatusChanged_To_Warehouse_Group(
        FulfillmentTaskStatus status,
        string expected)
    {
        var (broadcaster, hub) = Create();
        var taskId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var occurred = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);

        IDomainEvent domainEvent = status switch
        {
            FulfillmentTaskStatus.Assigned => new FulfillmentTaskAssigned(taskId, orderId, WarehouseId, Guid.NewGuid()) { OccurredOn = occurred },
            FulfillmentTaskStatus.Picking => new FulfillmentTaskPicking(taskId, orderId, WarehouseId) { OccurredOn = occurred },
            FulfillmentTaskStatus.Packed => new FulfillmentTaskPacked(taskId, orderId, WarehouseId) { OccurredOn = occurred },
            FulfillmentTaskStatus.Shipped => new FulfillmentTaskShipped(taskId, orderId, WarehouseId) { OccurredOn = occurred },
            FulfillmentTaskStatus.Cancelled => new FulfillmentTaskCancelled(taskId, orderId, WarehouseId, "duplicate") { OccurredOn = occurred },
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        await InvokeAsync(broadcaster, domainEvent, CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal($"wh:{WarehouseId}", groupKey);
        Assert.Equal(RealtimeEventTypes.TaskStatusChanged, envelope.Type);
        var data = Assert.IsType<TaskStatusChangedData>(envelope.Data);
        Assert.Equal(taskId, data.TaskId);
        Assert.Equal(orderId, data.OrderId);
        Assert.Equal(expected, data.Status);
    }

    [Fact]
    public async Task LowStockAlertRaised_Pushes_StockAlert_To_Warehouse_Group()
    {
        var (broadcaster, hub) = Create();
        var stockItemId = Guid.NewGuid();

        await broadcaster.HandleAsync(
            new LowStockAlertRaised(stockItemId, "SKU-1", WarehouseId, 3, 10)
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal($"wh:{WarehouseId}", groupKey);
        Assert.Equal(RealtimeEventTypes.StockAlert, envelope.Type);
        var data = Assert.IsType<StockAlertData>(envelope.Data);
        Assert.Equal(stockItemId, data.StockItemId);
        Assert.Equal("SKU-1", data.Sku);
        Assert.Equal(3, data.Available);
        Assert.Equal(10, data.Threshold);
    }

    private static Task InvokeAsync(WarehouseRealtimeBroadcaster broadcaster, IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        domainEvent switch
        {
            FulfillmentTaskCreated e => broadcaster.HandleAsync(e, cancellationToken),
            FulfillmentTaskAssigned e => broadcaster.HandleAsync(e, cancellationToken),
            FulfillmentTaskPicking e => broadcaster.HandleAsync(e, cancellationToken),
            FulfillmentTaskPacked e => broadcaster.HandleAsync(e, cancellationToken),
            FulfillmentTaskShipped e => broadcaster.HandleAsync(e, cancellationToken),
            FulfillmentTaskCancelled e => broadcaster.HandleAsync(e, cancellationToken),
            FulfillmentTaskSplit e => broadcaster.HandleAsync(e, cancellationToken),
            LowStockAlertRaised e => broadcaster.HandleAsync(e, cancellationToken),
            _ => Task.CompletedTask
        };
}

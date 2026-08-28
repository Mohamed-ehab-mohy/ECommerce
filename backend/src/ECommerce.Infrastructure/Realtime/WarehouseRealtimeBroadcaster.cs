using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Fans out fulfillment and stock events to the owning warehouse group (<c>wh:{id}</c>) on
/// <c>warehouseHub</c>: <c>NewFulfillmentTask</c>, <c>TaskStatusChanged</c> and <c>StockAlert</c>
///.
/// </summary>
public sealed class WarehouseRealtimeBroadcaster(
    IRealtimeEventForwarder forwarder,
    IWarehouseRealtimeHubContext warehouseHub) : IEventHandler<FulfillmentTaskCreated>,
        IEventHandler<FulfillmentTaskAssigned>,
        IEventHandler<FulfillmentTaskPicking>,
        IEventHandler<FulfillmentTaskPacked>,
        IEventHandler<FulfillmentTaskShipped>,
        IEventHandler<FulfillmentTaskCancelled>,
        IEventHandler<FulfillmentTaskSplit>,
        IEventHandler<LowStockAlertRaised>
{
    public Task HandleAsync(FulfillmentTaskCreated domainEvent, CancellationToken cancellationToken) =>
        forwarder.ForwardAsync(
            warehouseHub,
            $"wh:{domainEvent.WarehouseId}",
            RealtimeEventTypes.NewFulfillmentTask,
            new NewFulfillmentTaskData(domainEvent.TaskId, domainEvent.OrderId, domainEvent.Zone, domainEvent.Priority),
            domainEvent.OccurredOn,
            cancellationToken);

    public Task HandleAsync(FulfillmentTaskAssigned domainEvent, CancellationToken cancellationToken) =>
        ForwardStatusChangedAsync(domainEvent.TaskId, domainEvent.OrderId, domainEvent.WarehouseId, "Assigned", domainEvent.OccurredOn, cancellationToken);

    public Task HandleAsync(FulfillmentTaskPicking domainEvent, CancellationToken cancellationToken) =>
        ForwardStatusChangedAsync(domainEvent.TaskId, domainEvent.OrderId, domainEvent.WarehouseId, "Picking", domainEvent.OccurredOn, cancellationToken);

    public Task HandleAsync(FulfillmentTaskPacked domainEvent, CancellationToken cancellationToken) =>
        ForwardStatusChangedAsync(domainEvent.TaskId, domainEvent.OrderId, domainEvent.WarehouseId, "Packed", domainEvent.OccurredOn, cancellationToken);

    public Task HandleAsync(FulfillmentTaskShipped domainEvent, CancellationToken cancellationToken) =>
        ForwardStatusChangedAsync(domainEvent.TaskId, domainEvent.OrderId, domainEvent.WarehouseId, "Shipped", domainEvent.OccurredOn, cancellationToken);

    public Task HandleAsync(FulfillmentTaskCancelled domainEvent, CancellationToken cancellationToken) =>
        ForwardStatusChangedAsync(domainEvent.TaskId, domainEvent.OrderId, domainEvent.WarehouseId, "Cancelled", domainEvent.OccurredOn, cancellationToken);

    public Task HandleAsync(FulfillmentTaskSplit domainEvent, CancellationToken cancellationToken) =>
        ForwardStatusChangedAsync(domainEvent.TaskId, domainEvent.OrderId, domainEvent.WarehouseId, "Split", domainEvent.OccurredOn, cancellationToken);

    public Task HandleAsync(LowStockAlertRaised domainEvent, CancellationToken cancellationToken) =>
        forwarder.ForwardAsync(
            warehouseHub,
            $"wh:{domainEvent.WarehouseId}",
            RealtimeEventTypes.StockAlert,
            new StockAlertData(domainEvent.StockItemId, domainEvent.Sku, domainEvent.WarehouseId, domainEvent.Available, domainEvent.Threshold),
            domainEvent.OccurredOn,
            cancellationToken);

    private Task ForwardStatusChangedAsync(
        Guid taskId,
        Guid orderId,
        Guid warehouseId,
        string status,
        DateTime occurredAt,
        CancellationToken cancellationToken) =>
        forwarder.ForwardAsync(
            warehouseHub,
            $"wh:{warehouseId}",
            RealtimeEventTypes.TaskStatusChanged,
            new TaskStatusChangedData(taskId, orderId, status),
            occurredAt,
            cancellationToken);
}

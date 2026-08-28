using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Fans out order lifecycle events to the owning customer's group (<c>u:{userId}</c>) on
/// <c>orderHub</c>: <c>OrderStatusChanged</c> and <c>OrderTimelineUpdated</c>.
/// </summary>
public sealed class OrderRealtimeBroadcaster(
    IRealtimeEventForwarder forwarder,
    IOrderRealtimeHubContext orderHub) : IEventHandler<OrderStatusChanged>, IEventHandler<OrderTimelineUpdated>
{
    public Task HandleAsync(OrderStatusChanged domainEvent, CancellationToken cancellationToken) =>
        domainEvent.CustomerId is Guid customerId
            ? forwarder.ForwardAsync(
                orderHub,
                $"u:{customerId}",
                RealtimeEventTypes.OrderStatusChanged,
                new OrderStatusChangedData(domainEvent.OrderNumber, domainEvent.To.ToString()),
                domainEvent.OccurredOn,
                cancellationToken)
            : Task.CompletedTask;

    public Task HandleAsync(OrderTimelineUpdated domainEvent, CancellationToken cancellationToken) =>
        domainEvent.CustomerId is Guid customerId
            ? forwarder.ForwardAsync(
                orderHub,
                $"u:{customerId}",
                RealtimeEventTypes.OrderTimelineUpdated,
                new OrderTimelineUpdatedData(domainEvent.OrderNumber),
                domainEvent.OccurredOn,
                cancellationToken)
            : Task.CompletedTask;
}

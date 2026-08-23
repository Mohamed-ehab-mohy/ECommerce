using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Orders;

namespace ECommerce.Domain.Events;

/// <summary>
/// Raised whenever an order's status transitions; consumed by the real-time layer to push
/// <c>OrderStatusChanged</c> to the owning customer's group (US-N-001, FR-12).
/// </summary>
public sealed record OrderStatusChanged(
    Guid OrderId,
    string OrderNumber,
    Guid? CustomerId,
    OrderStatus From,
    OrderStatus To) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Raised whenever the order's timeline changes (status log entry or shipping-address correction);
/// consumed by the real-time layer to push <c>OrderTimelineUpdated</c> (US-N-001, FR-12).
/// </summary>
public sealed record OrderTimelineUpdated(
    Guid OrderId,
    string OrderNumber,
    Guid? CustomerId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

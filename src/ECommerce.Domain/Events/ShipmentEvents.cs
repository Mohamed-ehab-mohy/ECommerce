using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Fulfillment;

namespace ECommerce.Domain.Events;

public sealed record ShipmentCreated(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierKey,
    string TrackingNumber) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record ShipmentStatusChanged(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierKey,
    string TrackingNumber,
    ShipmentStatus Status) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record ShipmentDelivered(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierKey,
    string TrackingNumber) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

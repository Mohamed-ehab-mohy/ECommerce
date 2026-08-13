using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record OrderShipped(
    Guid OrderId,
    string OrderNumber,
    string CustomerEmail,
    string CarrierKey,
    IReadOnlyList<string> TrackingNumbers) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record OrderDelivered(
    Guid OrderId,
    string OrderNumber,
    string CustomerEmail) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

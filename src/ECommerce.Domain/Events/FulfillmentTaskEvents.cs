using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record FulfillmentTaskCreated(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId,
    string? Zone,
    int Priority) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record FulfillmentTaskAssigned(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId,
    Guid PickerId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record FulfillmentTaskPicking(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record FulfillmentTaskPacked(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record FulfillmentTaskShipped(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record FulfillmentTaskCancelled(
    Guid TaskId,
    Guid OrderId,
    Guid WarehouseId,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

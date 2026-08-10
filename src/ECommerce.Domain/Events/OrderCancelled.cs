using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record OrderCancelled(
    Guid OrderId,
    string OrderNumber,
    string CustomerEmail,
    decimal Total,
    string Currency,
    string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

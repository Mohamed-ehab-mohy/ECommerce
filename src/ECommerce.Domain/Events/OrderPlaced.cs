using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record OrderPlaced(
    Guid OrderId,
    Guid CheckoutId,
    Guid CartId,
    string CustomerEmail,
    decimal Total,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

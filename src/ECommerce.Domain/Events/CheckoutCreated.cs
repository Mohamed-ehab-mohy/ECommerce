using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CheckoutCreated(Guid CheckoutId, Guid CartId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

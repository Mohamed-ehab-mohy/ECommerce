using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CartItemAdded(Guid CartId, Guid ProductId, int Quantity) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

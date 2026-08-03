using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CartItemRemoved(Guid CartId, Guid ProductId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

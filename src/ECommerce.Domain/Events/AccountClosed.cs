using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record AccountClosed(Guid CustomerId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CartExpired(Guid CartId, string OwnerKey) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

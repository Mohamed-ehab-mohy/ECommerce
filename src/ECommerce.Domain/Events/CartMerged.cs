using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CartMerged(Guid CartId, Guid MergedFromCartId, int MergedItems) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record WishlistItemAdded(Guid WishlistId, Guid ProductId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

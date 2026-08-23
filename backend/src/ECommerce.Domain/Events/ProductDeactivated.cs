using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record ProductDeactivated(Guid ProductId, string Sku, string Slug) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

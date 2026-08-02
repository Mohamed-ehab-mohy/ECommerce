using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record ProductCreated(
    Guid ProductId,
    string Sku,
    string Slug,
    string Name,
    string Currency,
    decimal ListAmount,
    decimal? OfferAmount) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

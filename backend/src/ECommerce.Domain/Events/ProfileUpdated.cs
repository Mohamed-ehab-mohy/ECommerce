using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record ProfileUpdated(
    Guid CustomerId,
    string DisplayName,
    string? Phone,
    string Locale,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

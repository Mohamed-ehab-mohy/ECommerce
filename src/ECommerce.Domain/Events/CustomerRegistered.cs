using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CustomerRegistered(
    Guid CustomerId,
    string Email,
    string DisplayName,
    string Locale,
    string Currency,
    string VerificationToken,
    DateTime ExpiresAtUtc) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

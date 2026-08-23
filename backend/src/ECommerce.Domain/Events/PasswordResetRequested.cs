using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record PasswordResetRequested(
    Guid CustomerId,
    string Email,
    string DisplayName,
    string ResetToken,
    DateTime ExpiresAtUtc) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

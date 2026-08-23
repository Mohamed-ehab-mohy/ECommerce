using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record PasswordReset(
    Guid CustomerId,
    string Email,
    string DisplayName,
    string? NewVerificationToken,
    DateTime? NewVerificationTokenExpiresAtUtc) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

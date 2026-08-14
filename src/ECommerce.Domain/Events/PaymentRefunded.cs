using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record PaymentRefunded(
    Guid PaymentId,
    Guid? OrderId,
    decimal Amount,
    string Currency,
    string? ProviderReference) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

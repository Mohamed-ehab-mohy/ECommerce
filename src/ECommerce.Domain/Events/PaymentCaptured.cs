using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record PaymentCaptured(
    Guid PaymentId,
    Guid? OrderId,
    decimal Amount,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record RefundCompleted(
    Guid RefundId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string? ProviderReference) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record RefundRequested(
    Guid RefundId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

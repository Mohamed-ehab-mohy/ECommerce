using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record RefundExecuting(
    Guid RefundId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    int Attempt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

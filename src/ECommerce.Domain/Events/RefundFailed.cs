using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record RefundFailed(
    Guid RefundId,
    Guid PaymentId,
    decimal Amount,
    string? Detail) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

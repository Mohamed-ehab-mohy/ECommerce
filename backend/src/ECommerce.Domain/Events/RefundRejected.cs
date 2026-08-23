using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record RefundRejected(
    Guid RefundId,
    Guid OrderId,
    string Reason,
    Guid? RejectedBy) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

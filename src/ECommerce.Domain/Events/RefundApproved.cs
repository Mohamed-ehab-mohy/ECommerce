using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record RefundApproved(
    Guid RefundId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    Guid? ApprovedBy) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

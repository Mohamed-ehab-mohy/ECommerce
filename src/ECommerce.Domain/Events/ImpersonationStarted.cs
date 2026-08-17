using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record ImpersonationStarted(
    Guid ImpersonatorId,
    Guid TargetUserId,
    string ImpersonatorEmail,
    string TargetEmail) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

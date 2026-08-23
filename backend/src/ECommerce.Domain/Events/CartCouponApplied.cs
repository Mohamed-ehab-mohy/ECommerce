using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record CartCouponApplied(Guid CartId, string CouponCode) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record CartCouponRemoved(Guid CartId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

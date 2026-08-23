using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record PromotionCreated(Guid PromotionId, string Name) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record PromotionActivated(Guid PromotionId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record PromotionPaused(Guid PromotionId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record PromotionScheduled(Guid PromotionId, DateTime? StartsAt, DateTime? EndsAt) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record CouponCreated(Guid CouponId, string Code, Guid PromotionId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record CouponRedeemed(Guid CouponId, string Code, Guid OrderId, Guid CustomerId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record CouponExhausted(Guid CouponId, string Code) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

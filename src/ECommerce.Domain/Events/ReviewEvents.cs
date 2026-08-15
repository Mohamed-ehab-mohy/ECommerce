using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record ReviewSubmitted(
    Guid ReviewId,
    Guid ProductId,
    Guid CustomerId,
    int Rating,
    string Comment,
    bool VerifiedPurchase) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record ReviewPublished(
    Guid ReviewId,
    Guid ProductId,
    Guid CustomerId,
    int Rating,
    Guid? ModeratorId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record ReviewRejected(
    Guid ReviewId,
    Guid ProductId,
    Guid CustomerId,
    string Reason,
    Guid? ModeratorId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record ReviewRemoved(
    Guid ReviewId,
    Guid ProductId,
    Guid CustomerId,
    string Reason,
    Guid? ModeratorId) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record ReviewVoted(
    Guid ReviewId,
    Guid CustomerId,
    int HelpfulVotes) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

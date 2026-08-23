using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Reviews;

/// <summary>
/// One customer's vote on a published review (FRS-K-005). A customer may vote once per review;
/// repeating a vote changes its value, never creating a duplicate.
/// </summary>
public sealed class ReviewVote : BaseEntity<Guid>
{
    private ReviewVote()
    {
    }

    public Guid ReviewId { get; private set; }

    public Guid CustomerId { get; private set; }

    public ReviewVoteValue Value { get; private set; }

    public static ReviewVote Create(
        Guid reviewId,
        Guid customerId,
        ReviewVoteValue value,
        DateTime utcNow)
    {
        var vote = new ReviewVote
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            CustomerId = customerId,
            Value = value,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        return vote;
    }

    public void Change(ReviewVoteValue value, DateTime utcNow)
    {
        Value = value;
        UpdatedAt = utcNow;
    }
}

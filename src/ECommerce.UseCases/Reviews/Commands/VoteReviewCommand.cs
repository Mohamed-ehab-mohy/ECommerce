using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Commands;

/// <summary>Records (or changes) a customer's helpful/not-helpful vote on a published review (US-K-005).</summary>
public sealed class VoteReviewCommand(
    Guid reviewId,
    Guid customerId,
    bool helpful) : IRequest<Result<VoteReviewResponse>>
{
    public Guid ReviewId { get; } = reviewId;

    public Guid CustomerId { get; } = customerId;

    public bool Helpful { get; } = helpful;
}

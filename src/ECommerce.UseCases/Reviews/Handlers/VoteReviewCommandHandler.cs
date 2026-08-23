using ECommerce.Domain.Identity;
using ECommerce.Domain.Reviews;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>
/// Records or changes a customer's vote on a published review; one vote per customer per review (US-K-005).
/// </summary>
public sealed class VoteReviewCommandHandler(
    IProductReviewRepository reviews,
    IReviewVoteRepository votes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<VoteReviewCommand> validator) : IRequestHandler<VoteReviewCommand, Result<VoteReviewResponse>>
{
    public async Task<Result<VoteReviewResponse>> Handle(
        VoteReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return AuthorizationErrors.NotAuthenticated;
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<VoteReviewResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var review = await reviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            return ProductReviewErrors.ReviewNotFound;
        }

        if (review.Status != ProductReviewStatus.Published)
        {
            return ProductReviewErrors.ReviewNotPublished;
        }

        var value = request.Helpful ? ReviewVoteValue.Helpful : ReviewVoteValue.NotHelpful;
        var existing = await votes.GetAsync(request.ReviewId, request.CustomerId, cancellationToken);
        if (existing is null)
        {
            votes.Add(ReviewVote.Create(request.ReviewId, request.CustomerId, value, utcNow));
        }
        else if (existing.Value != value)
        {
            existing.Change(value, utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var helpfulVotes = await votes.CountHelpfulAsync(request.ReviewId, cancellationToken);

        return new VoteReviewResponse(request.ReviewId, helpfulVotes);
    }
}

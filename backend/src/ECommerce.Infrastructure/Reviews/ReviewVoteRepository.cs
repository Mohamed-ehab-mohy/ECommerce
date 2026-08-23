using ECommerce.Domain.Reviews;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Reviews.Ports;

namespace ECommerce.Infrastructure.Reviews;

public sealed class ReviewVoteRepository(ECommerceDbContext dbContext) : IReviewVoteRepository
{
    public Task<ReviewVote?> GetAsync(Guid reviewId, Guid customerId, CancellationToken cancellationToken) =>
        dbContext.Set<ReviewVote>()
            .SingleOrDefaultAsync(
                vote => vote.ReviewId == reviewId && vote.CustomerId == customerId,
                cancellationToken);

    public Task<int> CountHelpfulAsync(Guid reviewId, CancellationToken cancellationToken) =>
        dbContext.Set<ReviewVote>()
            .CountAsync(
                vote => vote.ReviewId == reviewId && vote.Value == ReviewVoteValue.Helpful,
                cancellationToken);

    public void Add(ReviewVote vote) => dbContext.Set<ReviewVote>().Add(vote);
}

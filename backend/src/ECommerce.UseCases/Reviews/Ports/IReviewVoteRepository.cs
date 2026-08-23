using ECommerce.Domain.Reviews;

namespace ECommerce.UseCases.Reviews.Ports;

public interface IReviewVoteRepository
{
    Task<ReviewVote?> GetAsync(Guid reviewId, Guid customerId, CancellationToken cancellationToken);

    Task<int> CountHelpfulAsync(Guid reviewId, CancellationToken cancellationToken);

    void Add(ReviewVote vote);
}

using ECommerce.Domain.Events;
using ECommerce.Domain.Reviews;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Search;

/// <summary>
/// Recomputes the product rating aggregate into the search read model whenever a review is
/// published or removed, keeping cached ratings fresh (FRS-K-003, US-K-003).
/// </summary>
public sealed class ReviewRatingSynchronizer(ECommerceDbContext dbContext)
    : IEventHandler<ReviewPublished>,
      IEventHandler<ReviewRemoved>
{
    public Task HandleAsync(ReviewPublished domainEvent, CancellationToken cancellationToken) =>
        RecomputeAsync(domainEvent.ProductId, cancellationToken);

    public Task HandleAsync(ReviewRemoved domainEvent, CancellationToken cancellationToken) =>
        RecomputeAsync(domainEvent.ProductId, cancellationToken);

    public async Task RecomputeAsync(Guid productId, CancellationToken cancellationToken)
    {
        var published = await dbContext.Set<ProductReview>()
            .Where(review => review.ProductId == productId && review.Status == ProductReviewStatus.Published)
            .Select(review => review.Rating)
            .ToListAsync(cancellationToken);

        var ratingCount = published.Count;
        var ratingAverage = ratingCount == 0
            ? 0m
            : Math.Round((decimal)published.Average(rating => rating), 2, MidpointRounding.AwayFromZero);

        var documents = await dbContext.Set<ProductSearchDocument>()
            .Where(document => document.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            document.RatingAverage = ratingAverage;
            document.RatingCount = ratingCount;
        }

        if (documents.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

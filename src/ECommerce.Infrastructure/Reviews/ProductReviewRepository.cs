using ECommerce.Domain.Reviews;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Reviews.Ports;

namespace ECommerce.Infrastructure.Reviews;

public sealed class ProductReviewRepository(ECommerceDbContext dbContext) : IProductReviewRepository
{
    public Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<ProductReview>()
            .SingleOrDefaultAsync(review => review.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductReview>> ListPublishedByProductAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        await dbContext.Set<ProductReview>()
            .Where(review => review.ProductId == productId && review.Status == ProductReviewStatus.Published)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductReview>> ListPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<ProductReview>()
            .Where(review => review.Status == ProductReviewStatus.Pending)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid productId, Guid customerId, CancellationToken cancellationToken) =>
        dbContext.Set<ProductReview>()
            .AnyAsync(review => review.ProductId == productId && review.CustomerId == customerId, cancellationToken);

    public void Add(ProductReview review) => dbContext.Set<ProductReview>().Add(review);
}

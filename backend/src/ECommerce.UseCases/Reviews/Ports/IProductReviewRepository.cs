using ECommerce.Domain.Reviews;

namespace ECommerce.UseCases.Reviews.Ports;

public interface IProductReviewRepository
{
    Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductReview>> ListPublishedByProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductReview>> ListPendingAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid productId, Guid customerId, CancellationToken cancellationToken);

    void Add(ProductReview review);
}

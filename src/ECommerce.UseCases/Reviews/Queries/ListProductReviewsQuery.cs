using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Queries;

/// <summary>Lists published reviews for a product with the aggregated rating (US-K-003).</summary>
public sealed class ListProductReviewsQuery(
    Guid productId,
    int page,
    int pageSize) : IRequest<Result<ProductReviewsResponse>>
{
    public Guid ProductId { get; } = productId;

    public int Page { get; } = page;

    public int PageSize { get; } = pageSize;
}

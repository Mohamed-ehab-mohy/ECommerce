using ECommerce.UseCases.Catalog.Ports;

namespace ECommerce.UseCases.Recommendations;

public interface IRecommendationService
{
    Task<IReadOnlyList<ProductRecommendation>> GetRecommendationsForUserAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRecommendation>> GetFrequentlyBoughtTogetherAsync(
        Guid productId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRecommendation>> GetTrendingProductsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);
}

public sealed record ProductRecommendation(
    Guid ProductId,
    string Sku,
    string Name,
    decimal Price,
    decimal Score,
    string Reason);

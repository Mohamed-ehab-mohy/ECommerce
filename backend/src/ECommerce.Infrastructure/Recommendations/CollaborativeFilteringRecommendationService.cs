using System.Collections.Concurrent;
using Dapper;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.ReadModels;
using ECommerce.UseCases.Recommendations;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Recommendations;

public sealed class CollaborativeFilteringRecommendationService(
    IDbConnectionFactory connectionFactory,
    ILogger<CollaborativeFilteringRecommendationService> logger) : IRecommendationService
{
    public async Task<IReadOnlyList<ProductRecommendation>> GetRecommendationsForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();
            var tenantId = TenantScope.Current;
            var userProducts = await GetUserPurchasedProductIdsAsync(connection, userId, tenantId);
            if (userProducts.Count == 0)
            {
                return await GetTrendingProductsAsync(connection, limit, cancellationToken);
            }

            var coOccurrence = await BuildCoOccurrenceMatrixAsync(connection, userProducts, tenantId);
            var scores = ComputeScores(userProducts, coOccurrence);

            var candidates = scores
                .Where(kvp => !userProducts.Contains(kvp.Key))
                .OrderByDescending(kvp => kvp.Value)
                .Take(limit)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();

            return candidates.Count == 0
                ? await GetTrendingProductsAsync(connection, limit, cancellationToken)
                : await HydrateProductsAsync(connection, candidates, tenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get recommendations for user {UserId}.", userId);
            return [];
        }
    }

    public async Task<IReadOnlyList<ProductRecommendation>> GetFrequentlyBoughtTogetherAsync(
        Guid productId,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();
            var tenantId = TenantScope.Current;
            var sql = """
                SELECT DISTINCT p2.product_id AS ProductId, COUNT(*) AS CoCount
                FROM order_items oi1
                JOIN orders o1 ON oi1.order_id = o1.id
                JOIN order_items oi2 ON oi2.order_id = o1.id AND oi2.product_id != oi1.product_id
                JOIN products p2 ON oi2.product_id = p2.id
                WHERE oi1.product_id = @ProductId
                  AND o1.tenant_id = @TenantId
                  AND p2.tenant_id = @TenantId
                GROUP BY p2.product_id
                ORDER BY CoCount DESC
                LIMIT @Limit
                """;

            var candidates = (await connection.QueryAsync<CoOccurrenceResult>(sql, new { ProductId = productId, TenantId = tenantId, Limit = limit })).ToList();
            return await HydrateProductsAsync(connection, candidates.Select(c => (c.ProductId, (double)c.CoCount)).ToList(), tenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get frequently bought together for product {ProductId}.", productId);
            return [];
        }
    }

    public async Task<IReadOnlyList<ProductRecommendation>> GetTrendingProductsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();
            return await GetTrendingProductsAsync(connection, limit, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get trending products.");
            return [];
        }
    }

    private static async Task<IReadOnlyList<ProductRecommendation>> GetTrendingProductsAsync(
        System.Data.IDbConnection connection,
        int limit,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantScope.Current;
        var sql = """
            SELECT oi.product_id AS ProductId, COUNT(DISTINCT o.id) AS CoCount
            FROM order_items oi
            JOIN orders o ON oi.order_id = o.id
            WHERE o.created_at >= @SinceDate
              AND o.tenant_id = @TenantId
            GROUP BY oi.product_id
            ORDER BY CoCount DESC
            LIMIT @Limit
            """;

        var sinceDate = DateTime.UtcNow.AddDays(-30);
        var candidates = (await connection.QueryAsync<CoOccurrenceResult>(sql, new { SinceDate = sinceDate, TenantId = tenantId, Limit = limit })).ToList();
        return await HydrateProductsAsync(connection, candidates.Select(c => (c.ProductId, (double)c.CoCount)).ToList(), tenantId);
    }

    private static async Task<HashSet<Guid>> GetUserPurchasedProductIdsAsync(
        System.Data.IDbConnection connection,
        Guid userId,
        Guid? tenantId)
    {
        var sql = """
            SELECT DISTINCT oi.product_id
            FROM order_items oi
            JOIN orders o ON oi.order_id = o.id
            WHERE o.customer_id = @UserId AND o.is_deleted = false
              AND o.tenant_id = @TenantId
            """;

        var ids = await connection.QueryAsync<Guid>(sql, new { UserId = userId, TenantId = tenantId });
        return ids.ToHashSet();
    }

    private static async Task<ConcurrentDictionary<Guid, int>> BuildCoOccurrenceMatrixAsync(
        System.Data.IDbConnection connection,
        HashSet<Guid> userProducts,
        Guid? tenantId)
    {
        var productArray = userProducts.ToArray();
        var coOccurrence = new ConcurrentDictionary<Guid, int>();

        var sql = """
            SELECT DISTINCT oi2.product_id AS ProductId, COUNT(*) AS CoCount
            FROM order_items oi1
            JOIN orders o ON oi1.order_id = o.id
            JOIN order_items oi2 ON oi2.order_id = o.id AND oi2.product_id != oi1.product_id
            WHERE oi1.product_id = ANY(@ProductIds)
              AND o.tenant_id = @TenantId
            GROUP BY oi2.product_id
            """;

        var results = await connection.QueryAsync<CoOccurrenceResult>(sql, new { ProductIds = productArray, TenantId = tenantId });
        foreach (var result in results)
        {
            coOccurrence.AddOrUpdate(result.ProductId, result.CoCount, (_, existing) => existing + result.CoCount);
        }

        return coOccurrence;
    }

    private static Dictionary<Guid, double> ComputeScores(
        HashSet<Guid> userProducts,
        ConcurrentDictionary<Guid, int> coOccurrence)
    {
        var scores = new Dictionary<Guid, double>();

        foreach (var (productId, coCount) in coOccurrence)
        {
            if (!userProducts.Contains(productId))
            {
                scores[productId] = coCount;
            }
        }

        var maxScore = scores.Values.DefaultIfEmpty(1).Max();
        foreach (var key in scores.Keys.ToList())
        {
            scores[key] /= maxScore;
        }

        return scores;
    }

    private static async Task<IReadOnlyList<ProductRecommendation>> HydrateProductsAsync(
        System.Data.IDbConnection connection,
        List<(Guid ProductId, double Score)> candidates,
        Guid? tenantId)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var productIds = candidates.Select(c => c.ProductId).ToArray();
        var sql = """
            SELECT p.id AS Id, p.sku AS Sku, pt.name AS Name,
                   COALESCE((SELECT pp.offer_amount FROM product_prices pp WHERE pp.product_id = p.id AND pp.is_deleted = false LIMIT 1),
                            (SELECT pp.list_amount FROM product_prices pp WHERE pp.product_id = p.id AND pp.is_deleted = false LIMIT 1), 0) AS Price
            FROM products p
            JOIN product_translations pt ON pt.product_id = p.id AND pt.locale = 'en'
            WHERE p.id = ANY(@Ids) AND p.is_deleted = false
              AND p.tenant_id = @TenantId
            """;

        var productMap = (await connection.QueryAsync<ProductInfo>(sql, new { Ids = productIds, TenantId = tenantId }))
            .ToDictionary(p => p.Id);

        var results = new List<ProductRecommendation>();
        foreach (var (productId, score) in candidates)
        {
            if (productMap.TryGetValue(productId, out var product))
            {
                results.Add(new ProductRecommendation(
                    productId,
                    product.Sku,
                    product.Name,
                    product.Price,
                    (decimal)score,
                    "collaborative-filtering"));
            }
        }

        return results;
    }

    private sealed class CoOccurrenceResult
    {
        public Guid ProductId { get; set; }
        public int CoCount { get; set; }
    }

    private sealed class ProductInfo
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}

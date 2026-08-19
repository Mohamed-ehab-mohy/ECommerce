using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Catalog.Ports;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Catalog;

public sealed class CachedProductRepository(
    IProductRepository inner,
    IConnectionMultiplexer redis,
    ILogger<CachedProductRepository> logger) : IProductRepository
{
    private const string SinglePrefix = "product:";
    private const string ListPrefix = "product:list:";
    private const string CountKey = "product:count";
    private const string ListKeysSet = "product:list-keys";

    private static readonly TimeSpan SingleTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CountTtl = TimeSpan.FromSeconds(60);

    private readonly IDatabase _cache = redis.GetDatabase();

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var key = new RedisKey($"{SinglePrefix}{id}");
        try
        {
            var cached = await _cache.StringGetAsync(key);
            if (!cached.IsNullOrEmpty)
            {
                return ProductCacheCodec.Deserialize(cached.ToString()!);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis read failed for product {ProductId}; falling back to DB.", id);
        }

        var product = await inner.GetByIdAsync(id, cancellationToken);
        if (product is not null)
        {
            await SetSingleAsync(key, product);
        }

        return product;
    }

    public async Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var key = new RedisKey($"{SinglePrefix}{id}");
        try
        {
            var cached = await _cache.StringGetAsync(key);
            if (!cached.IsNullOrEmpty)
            {
                var product = ProductCacheCodec.Deserialize(cached.ToString()!);
                return product.Status == ProductStatus.Active && !product.IsDeleted ? product : null;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis read failed for product {ProductId}; falling back to DB.", id);
        }

        var dbProduct = await inner.GetActiveByIdAsync(id, cancellationToken);
        if (dbProduct is not null)
        {
            await SetSingleAsync(key, dbProduct);
        }

        return dbProduct;
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var results = new List<Product>(ids.Count);
        var misses = new List<Guid>();

        foreach (var id in ids)
        {
            var key = new RedisKey($"{SinglePrefix}{id}");
            try
            {
                var cached = await _cache.StringGetAsync(key);
                if (!cached.IsNullOrEmpty)
                {
                    results.Add(ProductCacheCodec.Deserialize(cached.ToString()!));
                    continue;
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Redis read failed for product {ProductId}; falling back to DB.", id);
            }

            misses.Add(id);
        }

        if (misses.Count > 0)
        {
            var dbProducts = await inner.GetByIdsAsync(misses, cancellationToken);
            foreach (var product in dbProducts)
            {
                var key = new RedisKey($"{SinglePrefix}{product.Id}");
                await SetSingleAsync(key, product);
                results.Add(product);
            }
        }

        return results;
    }

    public Task<IReadOnlyList<Product>> GetBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken) =>
        inner.GetBySkusAsync(skus, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        inner.SkuExistsAsync(sku, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid excludeProductId, CancellationToken cancellationToken) =>
        inner.SlugExistsAsync(slug, excludeProductId, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        inner.SlugExistsAsync(slug, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListActiveAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var key = new RedisKey($"{ListPrefix}{page}:{pageSize}");
        try
        {
            var cached = await _cache.StringGetAsync(key);
            if (!cached.IsNullOrEmpty)
            {
                return ProductCacheCodec.DeserializeList(cached.ToString()!);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis read failed for product list page {Page}/{PageSize}; falling back to DB.", page, pageSize);
        }

        var items = await inner.ListActiveAsync(page, pageSize, cancellationToken);

        try
        {
            await _cache.StringSetAsync(key, ProductCacheCodec.SerializeList(items), ListTtl);
            await _cache.SetAddAsync(ListKeysSet, (RedisValue)key.ToString());
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis write failed for product list cache.");
        }

        return items;
    }

    public async Task<int> CountActiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _cache.StringGetAsync(CountKey);
            if (!cached.IsNullOrEmpty && int.TryParse(cached.ToString(), out var cachedCount))
            {
                return cachedCount;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis read failed for product count; falling back to DB.");
        }

        var count = await inner.CountActiveAsync(cancellationToken);

        try
        {
            await _cache.StringSetAsync(CountKey, count.ToString(), CountTtl);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis write failed for product count cache.");
        }

        return count;
    }

    public void Add(Product product) => inner.Add(product);

    public async Task InvalidateProductAsync(Guid productId)
    {
        try
        {
            await _cache.KeyDeleteAsync(new RedisKey($"{SinglePrefix}{productId}"));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis invalidation failed for product {ProductId}.", productId);
        }
    }

    public async Task InvalidateListCacheAsync()
    {
        try
        {
            var keys = await _cache.SetMembersAsync(ListKeysSet);
            if (keys.Length > 0)
            {
                var redisKeys = keys.Select(k => (RedisKey)k.ToString()).ToArray();
                await _cache.KeyDeleteAsync(redisKeys);
            }

            await _cache.KeyDeleteAsync(CountKey);
            await _cache.KeyDeleteAsync(ListKeysSet);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis list cache invalidation failed.");
        }
    }

    private async Task SetSingleAsync(RedisKey key, Product product)
    {
        try
        {
            await _cache.StringSetAsync(key, ProductCacheCodec.Serialize(product), SingleTtl);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis write failed for product {ProductId}.", product.Id);
        }
    }
}

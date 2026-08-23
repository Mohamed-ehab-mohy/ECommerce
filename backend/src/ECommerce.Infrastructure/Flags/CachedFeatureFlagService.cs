using System.Text.Json;
using ECommerce.Domain.Flags;
using ECommerce.UseCases.Flags.Ports;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Flags;

public sealed class CachedFeatureFlagService(
    IFeatureFlagRepository repository,
    IConnectionMultiplexer redis,
    ILogger<CachedFeatureFlagService> logger) : IFeatureFlagService
{
    private const string CachePrefix = "feature-flag:";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _cache = redis.GetDatabase();

    public async Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken)
    {
        var flag = await GetCachedAsync(key, cancellationToken);
        return flag is not null && flag.Enabled;
    }

    public async Task<string?> GetDescriptionAsync(string key, CancellationToken cancellationToken)
    {
        var flag = await GetCachedAsync(key, cancellationToken);
        return flag?.Description;
    }

    private async Task<FeatureFlag?> GetCachedAsync(string key, CancellationToken cancellationToken)
    {
        var cacheKey = new RedisKey($"{CachePrefix}{key}");

        try
        {
            var cached = await _cache.StringGetAsync(cacheKey);
            if (!cached.IsNullOrEmpty)
            {
                var dto = JsonSerializer.Deserialize<FeatureFlagCacheDto>(cached.ToString(), Options);
                return dto is { Key: { Length: > 0 } }
                    ? FeatureFlag.Rehydrate(dto.Key, dto.Description, dto.Enabled)
                    : null;
            }

            var flag = await repository.GetByKeyAsync(key, cancellationToken);
            var payload = flag is null
                ? new FeatureFlagCacheDto(string.Empty, string.Empty, false)
                : new FeatureFlagCacheDto(flag.Key, flag.Description, flag.Enabled);

            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(payload, Options), CacheTtl);
            return flag;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Feature flag cache access failed for key {Key}; falling back to repository.",
                key);
            return await repository.GetByKeyAsync(key, cancellationToken);
        }
    }

    private sealed record FeatureFlagCacheDto(string Key, string Description, bool Enabled);
}

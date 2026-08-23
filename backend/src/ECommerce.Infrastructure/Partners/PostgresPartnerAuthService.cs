using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Partners;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Partners;

public sealed class PostgresPartnerAuthService(
    IPartnerRepository partnerRepo,
    IConnectionMultiplexer redis) : IPartnerAuthService
{
    public async Task<PartnerAuthResult> AuthenticateAsync(string keyHash, CancellationToken cancellationToken)
    {
        var apiKey = await partnerRepo.GetByKeyHashAsync(keyHash, cancellationToken);
        if (apiKey is null || !apiKey.IsActive)
            return new PartnerAuthResult { IsAuthenticated = false };

        if (apiKey.ExpiresAt is { } expires && expires < DateTime.UtcNow)
            return new PartnerAuthResult { IsAuthenticated = false, IsExpired = true };

        var account = await partnerRepo.GetByIdAsync(apiKey.PartnerId, cancellationToken);

        var rateLimitRemaining = -1;
        var rateLimitPerMinute = 0;
        var isRateLimited = false;

        if (account?.RateLimitPerMinute is { } limit && limit > 0)
        {
            rateLimitPerMinute = limit;
            var rateLimitKey = $"partner:{apiKey.PartnerId}:rate";
            var db = redis.GetDatabase();
            var count = await db.StringIncrementAsync(rateLimitKey);
            if (count == 1)
            {
                await db.KeyExpireAsync(rateLimitKey, TimeSpan.FromMinutes(1));
            }

            rateLimitRemaining = Math.Max(0, limit - (int)count);
            isRateLimited = count > limit;
        }

        return new PartnerAuthResult
        {
            IsAuthenticated = !isRateLimited,
            IsRateLimited = isRateLimited,
            RateLimitRemaining = rateLimitRemaining,
            RateLimitPerMinute = rateLimitPerMinute,
            PartnerId = apiKey.PartnerId,
            ApiKeyId = apiKey.Id,
            PartnerName = apiKey.Name,
            Scopes = [.. apiKey.Scopes]
        };
    }

    public Task RecordUsageAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        return partnerRepo.RecordUsageAsync(apiKeyId, DateTime.UtcNow, cancellationToken);
    }
}

using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Common.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery<TResponse> cacheableQuery)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        var cacheKey = cacheableQuery.CacheKey;
        
        var cachedResponse = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedResponse is null)
        {
            _logger.LogInformation("Cache miss for {RequestName} with key {CacheKey}", requestName, cacheKey);
            
            var response = await next();
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheableQuery.Expiration ?? TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), options, cancellationToken);
            
            return response;
        }
        
        _logger.LogInformation("Cache hit for {RequestName} with key {CacheKey}", requestName, cacheKey);
        
        var parsedResponse = JsonSerializer.Deserialize<TResponse>(cachedResponse);
        return parsedResponse ?? await next();
    }
}

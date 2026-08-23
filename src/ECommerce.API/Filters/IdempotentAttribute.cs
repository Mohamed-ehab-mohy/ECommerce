using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // Ensure we only process POST, PUT, PATCH requests for idempotency
        if (request.Method == HttpMethods.Get || request.Method == HttpMethods.Delete || request.Method == HttpMethods.Head)
        {
            await next();
            return;
        }

        if (!request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new { Error = "Idempotency-Key header is required for this operation." });
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var cacheKey = $"Idempotency:{request.Path}:{idempotencyKey}";

        var cachedResponse = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedResponse))
        {
            var result = JsonSerializer.Deserialize<object>(cachedResponse);
            context.Result = new OkObjectResult(result) { StatusCode = 200 };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult && objectResult.StatusCode is >= 200 and < 300)
        {
            var serializedResponse = JsonSerializer.Serialize(objectResult.Value);
            await cache.SetStringAsync(cacheKey, serializedResponse, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            });
        }
    }
}

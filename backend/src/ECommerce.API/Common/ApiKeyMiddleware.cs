using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerce.UseCases.Partners;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.API.Common;

public sealed class ApiKeyMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string ApiKeyQuery = "api_key";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var headerValue)
            && !context.Request.Query.TryGetValue(ApiKeyQuery, out headerValue))
        {
            await next(context);
            return;
        }

        var rawKey = headerValue.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            await next(context);
            return;
        }

        var keyHash = HashKey(rawKey);
        var partnerAuth = context.RequestServices.GetRequiredService<IPartnerAuthService>();
        var result = await partnerAuth.AuthenticateAsync(keyHash, context.RequestAborted);

        if (!result.IsAuthenticated)
        {
            context.Response.StatusCode = 401;
            var error = result.IsExpired ? "API key expired" : "Invalid API key";
            await context.Response.WriteAsJsonAsync(new { error });
            return;
        }

        if (result.IsRateLimited)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new { error = "Partner rate limit exceeded" });
            return;
        }

        if (result.RateLimitPerMinute > 0)
        {
            context.Response.Headers["X-RateLimit-Limit"] = result.RateLimitPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = result.RateLimitRemaining.ToString();
        }

        await partnerAuth.RecordUsageAsync(result.ApiKeyId, context.RequestAborted);

        var claims = new List<Claim>
        {
            new("sub", result.PartnerId.ToString()),
            new("auth_type", "api_key"),
            new("api_key_id", result.ApiKeyId.ToString()),
            new("partner_name", result.PartnerName)
        };

        foreach (var scope in result.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await next(context);
    }

    internal static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

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
        var partnerRepo = context.RequestServices.GetRequiredService<IPartnerRepository>();
        var apiKey = await partnerRepo.GetByKeyHashAsync(keyHash, context.RequestAborted);

        if (apiKey is null || !apiKey.IsActive)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        if (apiKey.ExpiresAt is { } expires && expires < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key expired" });
            return;
        }

        await partnerRepo.RecordUsageAsync(apiKey.Id, DateTime.UtcNow, context.RequestAborted);

        var claims = new List<Claim>
        {
            new("sub", apiKey.PartnerId.ToString()),
            new("auth_type", "api_key"),
            new("api_key_id", apiKey.Id.ToString()),
            new("partner_name", apiKey.Name)
        };

        foreach (var scope in apiKey.Scopes)
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

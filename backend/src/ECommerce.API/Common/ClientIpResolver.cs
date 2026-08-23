using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Common;

public static class ClientIpResolver
{
    public static string Resolve(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress?.ToString();
        var isLoopback = remote is null or "::1" or "127.0.0.1";

        if (!isLoopback && !string.IsNullOrWhiteSpace(remote))
        {
            return remote;
        }

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return string.IsNullOrWhiteSpace(remote) ? "unknown" : remote;
    }
}

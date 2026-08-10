using System.Text.Json;
using ECommerce.Shared.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API;

public static class HealthResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            version = ApiVersionPolicy.CurrentVersion,
            generatedAtUtc = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, payload);
    }
}

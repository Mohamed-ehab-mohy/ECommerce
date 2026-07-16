using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var isDevelopment = context.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment();

        var statusCode = healthReport.Status switch
        {
            HealthStatus.Healthy => (int)HttpStatusCode.OK,
            HealthStatus.Degraded => (int)HttpStatusCode.OK,
            HealthStatus.Unhealthy => (int)HttpStatusCode.ServiceUnavailable,
            _ => (int)HttpStatusCode.OK
        };

        context.Response.StatusCode = statusCode;

        var response = new HealthCheckResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
            Checks = healthReport.Entries.Select(entry => new HealthCheckEntryResponse
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                Duration = entry.Value.Duration.TotalMilliseconds,
                Exception = isDevelopment ? entry.Value.Exception?.Message : null,
                ExceptionType = isDevelopment ? entry.Value.Exception?.GetType().FullName : null,
                Data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
            }).ToList()
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions));
    }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public double TotalDuration { get; set; }
    public List<HealthCheckEntryResponse> Checks { get; set; } = [];
}

public class HealthCheckEntryResponse
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Duration { get; set; }
    public string? Exception { get; set; }
    public string? ExceptionType { get; set; }
    public IReadOnlyDictionary<string, object>? Data { get; set; }
}

using System.Text.Json;
using ECommerce.Shared.Api;

namespace ECommerce.API.Common;

public sealed class ApiVersionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var version = ApiVersionPolicy.VersionSegment(path);

        if (version is not null && !ApiVersionPolicy.IsCurrentVersion(version))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "API version not found.",
                status = StatusCodes.Status404NotFound,
                instance = path
            });
            return;
        }

        context.Response.OnStarting(() =>
        {
            if (version is not null)
            {
                context.Response.Headers["X-API-Version"] = ApiVersionPolicy.CurrentVersion;
            }

            if (ApiVersionPolicy.IsDeprecatedPath(path))
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = ApiVersionPolicy.DeprecationSunset;
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

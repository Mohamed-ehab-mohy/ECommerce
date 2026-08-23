namespace ECommerce.API.Common;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["X-XSS-Protection"] = "0";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
            var isUiPath = context.Request.Path.StartsWithSegments("/swagger") || 
                           context.Request.Path.StartsWithSegments("/hangfire") ||
                           context.Request.Path.StartsWithSegments("/graphql");

            if (!isUiPath)
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }
            else
            {
                headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' ws: wss:;";
            }

            await next();
        });
    }
}

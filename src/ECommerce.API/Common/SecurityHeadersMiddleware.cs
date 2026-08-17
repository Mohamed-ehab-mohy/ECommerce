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
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            if (!context.Request.Path.StartsWithSegments("/hangfire") &&
                !context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }

            await next();
        });
    }
}

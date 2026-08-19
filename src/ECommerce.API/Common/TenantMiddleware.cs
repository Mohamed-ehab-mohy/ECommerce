using System.Security.Claims;

namespace ECommerce.API.Common;

public sealed class TenantMiddleware(RequestDelegate next)
{
    private const string TenantHeader = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        Guid? tenantId = null;

        if (context.Request.Headers.TryGetValue(TenantHeader, out var headerValue)
            && Guid.TryParse(headerValue.FirstOrDefault(), out var headerTenantId))
        {
            tenantId = headerTenantId;
        }
        else if (context.User?.FindFirstValue("tenant_id") is string claimValue
            && Guid.TryParse(claimValue, out var claimTenantId))
        {
            tenantId = claimTenantId;
        }

        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
        }

        await next(context);
    }
}

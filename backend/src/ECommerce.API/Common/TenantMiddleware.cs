using System.Security.Claims;
using ECommerce.Infrastructure.Common;

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
        else
        {
            // Resolve by Hostname
            var host = context.Request.Host.Host.ToLowerInvariant();
            if (!string.IsNullOrEmpty(host) && host != "localhost" && host != "127.0.0.1")
            {
                // We need to resolve ECommerceDbContext from DI to check domains
                var dbContext = context.RequestServices.GetService<ECommerce.Infrastructure.Data.ECommerceDbContext>();
                if (dbContext != null)
                {
                    // Cache should ideally be used here in production to avoid DB hits on every request
                    var tenant = dbContext.Tenants.FirstOrDefault(t => t.CustomDomain == host || host.StartsWith(t.Subdomain + "."));
                    if (tenant != null)
                    {
                        tenantId = tenant.Id;
                    }
                }
            }
        }

        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
        }

        TenantScope.Current = tenantId;

        try
        {
            await next(context);
        }
        finally
        {
            TenantScope.Current = null;
        }
    }
}

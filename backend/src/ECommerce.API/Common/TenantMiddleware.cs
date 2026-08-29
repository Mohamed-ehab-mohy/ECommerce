using System.Security.Claims;
using ECommerce.Infrastructure.Common;

namespace ECommerce.API.Common;

public sealed class TenantMiddleware(RequestDelegate next)
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string TenantClaim = "tenant_id";

    public async Task InvokeAsync(HttpContext context)
    {
        Guid? tenantId = null;

        // The request's X-Tenant-Id override, if any.
        Guid? headerTenantId = null;
        if (context.Request.Headers.TryGetValue(TenantHeader, out var headerValue)
            && Guid.TryParse(headerValue.FirstOrDefault(), out var parsedHeaderTenantId))
        {
            headerTenantId = parsedHeaderTenantId;
        }

        // For an authenticated principal carrying a tenant_id claim, the claim is
        // authoritative. This prevents a user from traversing into another tenant's
        // data simply by supplying an X-Tenant-Id belonging to a different tenant.
        if (context.User?.Identity?.IsAuthenticated == true
            && context.User.FindFirstValue(TenantClaim) is string claimValue
            && Guid.TryParse(claimValue, out var claimTenantId))
        {
            if (headerTenantId.HasValue && headerTenantId.Value != claimTenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title = "Forbidden",
                    status = StatusCodes.Status403Forbidden,
                    detail = "The supplied tenant does not match the authenticated user's tenant."
                });
                return;
            }

            tenantId = claimTenantId;
        }
        else if (headerTenantId.HasValue)
        {
            tenantId = headerTenantId;
        }
        else if (context.User?.FindFirstValue(TenantClaim) is string claimValue2
            && Guid.TryParse(claimValue2, out var claimTenantId2))
        {
            tenantId = claimTenantId2;
        }
        else
        {
            // Resolve by Hostname
            var host = context.Request.Host.Host.ToLowerInvariant();
            if (!string.IsNullOrEmpty(host) && host != "localhost" && host != "127.0.0.1")
            {
                var sender = context.RequestServices.GetService<MediatR.ISender>();
                if (sender != null)
                {
                    var result = await sender.Send(new ECommerce.UseCases.Tenants.Queries.GetTenantIdByDomainQuery(host));
                    if (result.IsSuccess)
                    {
                        tenantId = result.Value;
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

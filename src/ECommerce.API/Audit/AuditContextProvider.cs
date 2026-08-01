using System.Diagnostics;
using System.Security.Claims;
using ECommerce.API.Common;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit.Ports;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Audit;

public sealed class AuditContextProvider(IHttpContextAccessor httpContextAccessor) : IAuditContextProvider
{
    public AuditContext Get()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new AuditContext(null, null, null, null, null);
        }

        var userId = httpContext.User.FindFirstValue("sub");
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        return new AuditContext(
            userId is null ? null : Guid.Parse(userId),
            AuditActorType.User,
            ClientIpResolver.Resolve(httpContext),
            httpContext.Request.Headers.UserAgent.ToString(),
            traceId);
    }
}

using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.API.Common;

public sealed class TenantAwareOutputCachePolicy : IOutputCachePolicy
{
    public static readonly TenantAwareOutputCachePolicy Instance = new();

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;

        var tenantId = context.HttpContext.Items["TenantId"] is Guid tenantIdGuid
            ? tenantIdGuid.ToString()
            : "null";

        context.CacheVaryByRules.VaryByValues["tenant"] = tenantId;
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}

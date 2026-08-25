using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Common;

public sealed class HttpContextTenantService(IHttpContextAccessor accessor) : ITenantService
{
    public Guid? GetCurrentTenantId()
    {
        return accessor.HttpContext?.Items["TenantId"] is Guid tenantId ? tenantId : null;
    }

    public void SetCurrentTenantId(Guid? tenantId)
    {
        if (accessor.HttpContext != null)
        {
            if (tenantId.HasValue)
                accessor.HttpContext.Items["TenantId"] = tenantId.Value;
            else
                accessor.HttpContext.Items.Remove("TenantId");
        }
    }
}

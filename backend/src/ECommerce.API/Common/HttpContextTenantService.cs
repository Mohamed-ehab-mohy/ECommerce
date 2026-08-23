using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Common;

public sealed class HttpContextTenantService(IHttpContextAccessor accessor) : ITenantService
{
    public Guid? GetCurrentTenantId()
    {
        return accessor.HttpContext?.Items["TenantId"] is Guid tenantId ? tenantId : null;
    }
}

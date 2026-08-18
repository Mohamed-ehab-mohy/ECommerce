using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Common;

public sealed class HttpContextCorrelationIdProvider(IHttpContextAccessor accessor) : ICorrelationIdProvider
{
    public string CorrelationId =>
        accessor.HttpContext?.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("D");
}

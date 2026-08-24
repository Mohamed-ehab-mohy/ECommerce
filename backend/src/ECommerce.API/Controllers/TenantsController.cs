using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ECommerce.UseCases.Tenants.Commands;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController(ISender sender) : ControllerBase
{
    // Normally, this endpoint would either be public (for self-serve SaaS signup)
    // or protected by a SuperAdmin policy. For SaaS MVP, let's keep it public/unauthorized.
    [HttpPost]
    public async Task<IActionResult> CreateTenant(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTenantCommand(
            request.Name,
            request.Subdomain,
            request.CustomDomain);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/tenants/{result.Value.Id}", result.Value)
            : BadRequest(result.Error);
    }
}

public sealed record CreateTenantRequest(
    string Name,
    string Subdomain,
    string? CustomDomain);

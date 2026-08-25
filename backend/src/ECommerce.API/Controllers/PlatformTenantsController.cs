using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Platform.Commands;
using ECommerce.UseCases.Platform.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/platform/tenants")]
[Authorize(Roles = "SuperAdmin")] // Restrict to platform owners
public class PlatformTenantsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTenants()
    {
        var result = await sender.Send(new ListAllTenantsQuery());
        return Ok(result.Value);
    }

    [HttpPost("{tenantId:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendTenant(Guid tenantId)
    {
        var result = await sender.Send(new SuspendTenantCommand(tenantId));
        return result.IsFailure ? NotFound(result.Error) : Ok();
    }

    [HttpPost("{tenantId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTenant(Guid tenantId)
    {
        var result = await sender.Send(new ActivateTenantCommand(tenantId));
        return result.IsFailure ? NotFound(result.Error) : Ok();
    }
}

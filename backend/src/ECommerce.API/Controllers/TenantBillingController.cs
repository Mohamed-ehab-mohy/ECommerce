using ECommerce.UseCases.Tenants.Commands;
using ECommerce.UseCases.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/billing")]
[Authorize(Roles = "Admin")] // Restricted to Tenant Admin
public sealed class TenantBillingController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBillingSummaryQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("change-plan")]
    public async Task<IActionResult> ChangePlan([FromBody] ChangePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeSubscriptionPlanCommand(request.PlanId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

public sealed record ChangePlanRequest(Guid PlanId);

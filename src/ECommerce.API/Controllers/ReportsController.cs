using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    /// <summary>Sales time-series report (reports.read, US-L-001).</summary>
    [HttpGet("sales")]
    public async Task<IActionResult> Sales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? granularity,
        [FromQuery] string? currency,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SalesReportQuery(from, to, granularity, currency),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Inventory position report (reports.read, US-L-002).</summary>
    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new InventoryReportQuery(), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Finance report matching the payment ledger (reports.read, US-L-006).</summary>
    [HttpGet("finance")]
    public async Task<IActionResult> Finance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new FinanceReportQuery(from, to),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Promotion performance report (reports.read, US-L-004).</summary>
    [HttpGet("promotions")]
    public async Task<IActionResult> Promotions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PromotionReportQuery(from, to),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Fulfillment SLA report (reports.read, US-L-005).</summary>
    [HttpGet("fulfillment")]
    public async Task<IActionResult> Fulfillment(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new FulfillmentReportQuery(from, to),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

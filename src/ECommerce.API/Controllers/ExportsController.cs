using ECommerce.API.Common;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Commands;
using ECommerce.UseCases.Reports.Ports;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/exports")]
public sealed class ExportsController(ISender sender, IExportFileStore fileStore) : ControllerBase
{
    /// <summary>Starts an async report export (reports.read, US-L-007).</summary>
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] CreateExportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateExportCommand(request.ReportType, request.From, request.To, request.Granularity, request.Currency),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Accepted($"/api/v1/exports/{result.Value.ExportId}", result.Value);
    }

    /// <summary>Returns the export status and, once Completed, the download URL (reports.read).</summary>
    [HttpGet("{exportId:guid}")]
    public async Task<IActionResult> Get(Guid exportId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExportQuery(exportId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Streams the generated CSV file when the export has Completed (reports.read).</summary>
    [HttpGet("{exportId:guid}/download")]
    public async Task<IActionResult> Download(Guid exportId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExportQuery(exportId), cancellationToken);
        if (result.IsFailure)
        {
            return ProblemResponse.Create(result.ToOperationError());
        }

        var export = result.Value;
        if (export.Status != "Completed" || export.FileKey is null)
        {
            return ProblemResponse.Create(new Error(
                "Reporting.ExportNotReady",
                "The export has not completed yet.",
                ErrorType.Conflict).ToOperationError());
        }

        var content = await fileStore.GetAsync(export.FileKey, cancellationToken);

        return content is not null
            ? File(content, "text/csv", $"{export.ReportType}-{export.ExportId:N}.csv")
            : ProblemResponse.Create(new Error(
                "Reporting.ExportFileMissing",
                "The export file is missing from storage.",
                ErrorType.NotFound).ToOperationError());
    }
}

public sealed record CreateExportRequest(
    string ReportType,
    DateTime? From,
    DateTime? To,
    string? Granularity,
    string? Currency);

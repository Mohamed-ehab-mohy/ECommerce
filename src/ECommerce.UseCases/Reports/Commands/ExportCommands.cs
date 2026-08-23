using ECommerce.Domain.Reporting;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UseCases.Reports.Commands;

/// <summary>Starts an async report export job (US-L-007, docs/08 §6.9).</summary>
public sealed record CreateExportCommand(
    string ReportType,
    DateTime? From,
    DateTime? To,
    string? Granularity,
    string? Currency)
    : IRequest<Result<ExportStartedResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}

/// <summary>Returns the status and (once Completed) file key of an export job (docs/08 §6.9).</summary>
public sealed record GetExportQuery(Guid ExportId)
    : IRequest<Result<ExportStatusResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}

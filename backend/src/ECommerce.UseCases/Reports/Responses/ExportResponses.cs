namespace ECommerce.UseCases.Reports.Responses;

public sealed record ExportStartedResponse(Guid ExportId, string Status);

public sealed record ExportStatusResponse(
    Guid ExportId,
    string ReportType,
    string Status,
    int RowCount,
    string? FileKey,
    Guid? CreatedBy,
    DateTime CreatedAt,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

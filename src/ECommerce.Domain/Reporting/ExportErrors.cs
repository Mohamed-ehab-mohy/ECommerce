using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Reporting;

public static class ExportErrors
{
    public static readonly Error ExportNotFound = new(
        "Reporting.ExportNotFound",
        "The export job was not found.",
        ErrorType.NotFound);

    public static readonly Error ExportNotReady = new(
        "Reporting.ExportNotReady",
        "The export has not completed yet.",
        ErrorType.Conflict);

    public static readonly Error ExportFileMissing = new(
        "Reporting.ExportFileMissing",
        "The export file is missing from storage.",
        ErrorType.NotFound);
}

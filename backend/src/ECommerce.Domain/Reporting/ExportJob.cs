using ECommerce.Domain.Common;

namespace ECommerce.Domain.Reporting;

public enum ExportJobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

/// <summary>
/// An async report export job. The generated file is written to storage and
/// referenced by <see cref="FileKey"/> so the status endpoint can stream it back.
/// </summary>
public sealed class ExportJob : BaseEntity<Guid>
{
    private ExportJob()
    {
        ReportType = string.Empty;
        FiltersJson = string.Empty;
    }

    public string ReportType { get; private set; }

    /// <summary>Serialized report filters (from/to/granularity/currency) so the async job can re-run the query.</summary>
    public string FiltersJson { get; private set; }

    public ExportJobStatus Status { get; private set; }

    public int RowCount { get; private set; }

    /// <summary>Relative file key in the export store (exposed only while Completed).</summary>
    public string? FileKey { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public static ExportJob Create(string reportType, string filtersJson, Guid? createdBy, DateTime utcNow)
    {
        return new ExportJob
        {
            Id = Guid.NewGuid(),
            ReportType = reportType,
            FiltersJson = filtersJson,
            Status = ExportJobStatus.Queued,
            CreatedBy = createdBy,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void MarkRunning(DateTime utcNow)
    {
        Status = ExportJobStatus.Running;
        StartedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public void Complete(int rowCount, string fileKey, DateTime utcNow)
    {
        Status = ExportJobStatus.Completed;
        RowCount = rowCount;
        FileKey = fileKey;
        CompletedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public void Fail(DateTime utcNow)
    {
        Status = ExportJobStatus.Failed;
        CompletedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }
}

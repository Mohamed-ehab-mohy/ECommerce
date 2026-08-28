using ECommerce.Domain.Common;

namespace ECommerce.Domain.Catalog;

public enum ProductImportStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// A bulk product import batch (FRS-B-007). Rows are stored as JSON, validated per row,
/// and the run records per-row errors for partial success.
/// </summary>
public sealed class ProductImport : BaseEntity<Guid>
{
    private readonly List<ProductImportError> _errors = [];

    private ProductImport()
    {
        RowsJson = string.Empty;
    }

    public ProductImportStatus Status { get; private set; }

    /// <summary>Serialized batch rows (JSON array) so the async job can re-read them.</summary>
    public string RowsJson { get; private set; }

    public int TotalRows { get; private set; }

    public int SucceededRows { get; private set; }

    public int FailedRows { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public IReadOnlyCollection<ProductImportError> Errors => _errors;

    public static ProductImport Create(string rowsJson, int totalRows, DateTime utcNow)
    {
        var import = new ProductImport
        {
            Id = Guid.NewGuid(),
            Status = ProductImportStatus.Queued,
            RowsJson = rowsJson,
            TotalRows = totalRows,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        return import;
    }

    public void MarkProcessing(DateTime utcNow)
    {
        Status = ProductImportStatus.Processing;
        StartedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public void AddSucceeded()
    {
        SucceededRows++;
    }

    public void AddError(int rowNumber, string sku, string message, DateTime utcNow)
    {
        _errors.Add(new ProductImportError
        {
            Id = Guid.NewGuid(),
            ProductImportId = Id,
            RowNumber = rowNumber,
            Sku = sku,
            Message = message,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });
        FailedRows++;
        UpdatedAt = utcNow;
    }

    public void Complete(DateTime utcNow)
    {
        Status = ProductImportStatus.Completed;
        FinishedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public void Fail(DateTime utcNow)
    {
        Status = ProductImportStatus.Failed;
        FinishedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }
}

/// <summary>A single per-row import error for the error report.</summary>
public sealed class ProductImportError
{
    public Guid Id { get; internal set; }

    public Guid ProductImportId { get; internal set; }

    public int RowNumber { get; internal set; }

    public string Sku { get; internal set; } = string.Empty;

    public string Message { get; internal set; } = string.Empty;

    public DateTime CreatedAt { get; internal set; }

    public DateTime UpdatedAt { get; internal set; }
}

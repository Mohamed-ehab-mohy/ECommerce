namespace ECommerce.UseCases.Catalog.Responses;

public sealed record ProductImportStartedResponse(Guid ImportId, string Status);

public sealed record ProductImportErrorResponse(int RowNumber, string Sku, string Message);

public sealed record ProductImportStatusResponse(
    Guid ImportId,
    string Status,
    int TotalRows,
    int SucceededRows,
    int FailedRows,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<ProductImportErrorResponse> Errors);

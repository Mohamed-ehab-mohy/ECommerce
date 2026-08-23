namespace ECommerce.UseCases.Catalog.Responses;

public sealed record BulkProductStatusItemResponse(
    Guid ProductId,
    string? Sku,
    bool Success,
    string? Error);

public sealed record BulkProductStatusChangeResponse(
    int Processed,
    int Succeeded,
    int Failed,
    IReadOnlyList<BulkProductStatusItemResponse> Items);

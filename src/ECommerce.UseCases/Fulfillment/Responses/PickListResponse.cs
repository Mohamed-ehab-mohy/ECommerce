namespace ECommerce.UseCases.Fulfillment.Responses;

public sealed record PickListLineResponse(
    Guid TaskId,
    string OrderNumber,
    string Sku,
    string? BinLocation,
    int Quantity);

public sealed record PickListResponse(
    string Zone,
    string WarehouseCode,
    int LineCount,
    int TotalItems,
    IReadOnlyList<PickListLineResponse> Lines);

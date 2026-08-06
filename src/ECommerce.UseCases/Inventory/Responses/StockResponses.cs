using ECommerce.Domain.Inventory;

namespace ECommerce.UseCases.Inventory.Responses;

public sealed record StockItemResponse(
    Guid Id,
    string Sku,
    Guid WarehouseId,
    int OnHand,
    int Allocated,
    int Available);

public sealed record PagedStockItemsResponse(
    IReadOnlyList<StockItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record StockMovementResponse(
    Guid Id,
    Guid StockItemId,
    string Type,
    int Quantity,
    int OnHandDelta,
    int AllocatedDelta,
    string Reason,
    string? Reference,
    string? Note,
    DateTime CreatedAt);

public sealed record PagedStockMovementsResponse(
    IReadOnlyList<StockMovementResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public static class StockResponseMappings
{
    public static StockItemResponse ToResponse(this StockItem item) =>
        new(item.Id, item.Sku, item.WarehouseId, item.OnHand, item.Allocated, item.Available);

    public static StockMovementResponse ToResponse(this StockMovement movement) =>
        new(
            movement.Id,
            movement.StockItemId,
            movement.Type.ToString(),
            movement.Quantity,
            movement.OnHandDelta,
            movement.AllocatedDelta,
            movement.Reason,
            movement.Reference,
            movement.Note,
            movement.CreatedAt);
}

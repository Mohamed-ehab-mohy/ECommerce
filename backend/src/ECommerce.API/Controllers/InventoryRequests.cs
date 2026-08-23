namespace ECommerce.API.Controllers;

public sealed record CreateWarehouseRequest(
    string Code,
    string Name,
    string Address,
    string Timezone,
    string? Status);

public sealed record UpdateWarehouseRequest(
    string? Name,
    string? Address,
    string? Timezone,
    string? Status);

public sealed record PostStockMovementRequest(
    string Sku,
    Guid WarehouseId,
    string Type,
    int Quantity,
    string Reason,
    string? Reference,
    string? Note,
    Guid? ApprovedBy);

public sealed record TransferStockRequest(
    string Sku,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity,
    string? Note);

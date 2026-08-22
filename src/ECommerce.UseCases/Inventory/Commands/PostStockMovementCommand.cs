using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed record PostStockMovementCommand(
    string Sku,
    Guid WarehouseId,
    string Type,
    int Quantity,
    string Reason,
    string? Reference,
    string? Note,
    Guid? ApprovedBy = null) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.InventoryStockWrite;
}

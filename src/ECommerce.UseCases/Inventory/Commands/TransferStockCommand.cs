using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed record TransferStockCommand(
    string Sku,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity,
    string? Note) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.InventoryStockWrite;
}

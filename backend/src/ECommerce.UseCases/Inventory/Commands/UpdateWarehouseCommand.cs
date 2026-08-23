using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed record UpdateWarehouseCommand(
    Guid WarehouseId,
    string? Name,
    string? Address,
    string? Timezone,
    string? Status) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.InventoryWarehouseWrite;
}

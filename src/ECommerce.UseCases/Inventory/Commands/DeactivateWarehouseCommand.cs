using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.InventoryWarehouseDelete;
}

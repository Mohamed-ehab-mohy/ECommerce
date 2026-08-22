using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Queries;

public sealed record GetWarehouseQuery(Guid WarehouseId) : IRequest<Result<WarehouseResponse>>, IRequirePermission
{
    public string Permission => Permissions.InventoryWarehouseRead;
}

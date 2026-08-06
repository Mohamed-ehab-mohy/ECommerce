using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed record CreateWarehouseCommand(
    string Code,
    string Name,
    string Address,
    string Timezone,
    string? Status) : IRequest<Result<Guid>>, IRequirePermission
{
    public string Permission => Permissions.InventoryWarehouseWrite;
}

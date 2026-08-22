using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Queries;

public sealed record ListStockItemsQuery(int Page = 1, int PageSize = 20, Guid? WarehouseId = null)
    : IRequest<Result<PagedStockItemsResponse>>, IRequirePermission
{
    public string Permission => Permissions.InventoryStockRead;
}

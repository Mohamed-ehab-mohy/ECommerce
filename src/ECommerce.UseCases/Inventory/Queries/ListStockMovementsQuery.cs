using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Queries;

public sealed record ListStockMovementsQuery(Guid StockItemId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedStockMovementsResponse>>, IRequirePermission
{
    public string Permission => Permissions.InventoryStockRead;
}

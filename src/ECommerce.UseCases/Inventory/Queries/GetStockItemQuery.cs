using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Queries;

public sealed record GetStockItemQuery(Guid StockItemId) : IRequest<Result<StockItemResponse>>, IRequirePermission
{
    public string Permission => Permissions.InventoryStockRead;
}

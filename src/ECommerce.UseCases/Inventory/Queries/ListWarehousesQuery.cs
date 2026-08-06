using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Responses;
using MediatR;

namespace ECommerce.UseCases.Inventory.Queries;

public sealed record ListWarehousesQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedWarehousesResponse>>, IRequirePermission
{
    public string Permission => Permissions.InventoryWarehouseRead;
}

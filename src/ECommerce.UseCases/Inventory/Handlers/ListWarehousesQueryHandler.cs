using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Inventory.Queries;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class ListWarehousesQueryHandler(
    IWarehouseRepository warehouses,
    IValidator<ListWarehousesQuery> validator) : IRequestHandler<ListWarehousesQuery, Result<PagedWarehousesResponse>>
{
    public async Task<Result<PagedWarehousesResponse>> Handle(ListWarehousesQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedWarehousesResponse>();
        }

        var items = await warehouses.ListAsync(request.Page, request.PageSize, cancellationToken);
        var total = await warehouses.CountAsync(cancellationToken);

        return Result<PagedWarehousesResponse>.Success(new PagedWarehousesResponse(
            items.Select(GetWarehouseQueryHandler.ToResponse).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}

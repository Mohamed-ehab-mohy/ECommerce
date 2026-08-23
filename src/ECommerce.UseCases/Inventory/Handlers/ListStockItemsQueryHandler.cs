using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Inventory.Queries;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class ListStockItemsQueryHandler(
    IStockRepository stock,
    IValidator<ListStockItemsQuery> validator) : IRequestHandler<ListStockItemsQuery, Result<PagedStockItemsResponse>>
{
    public async Task<Result<PagedStockItemsResponse>> Handle(ListStockItemsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedStockItemsResponse>();
        }

        var items = await stock.ListAsync(request.Page, request.PageSize, request.WarehouseId, cancellationToken);
        var total = await stock.CountAsync(request.WarehouseId, cancellationToken);

        return Result<PagedStockItemsResponse>.Success(new PagedStockItemsResponse(
            items.Select(item => item.ToResponse()).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}

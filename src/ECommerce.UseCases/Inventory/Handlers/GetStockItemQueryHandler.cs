using ECommerce.Domain.Inventory;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Inventory.Queries;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class GetStockItemQueryHandler(
    IStockRepository stock,
    IValidator<GetStockItemQuery> validator) : IRequestHandler<GetStockItemQuery, Result<StockItemResponse>>
{
    public async Task<Result<StockItemResponse>> Handle(GetStockItemQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<StockItemResponse>();
        }

        var stockItem = await stock.GetByIdAsync(request.StockItemId, cancellationToken);

        return stockItem is null
            ? Result<StockItemResponse>.Failure(StockErrors.StockItemNotFound)
            : Result<StockItemResponse>.Success(stockItem.ToResponse());
    }
}

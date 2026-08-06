using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Inventory.Queries;
using ECommerce.UseCases.Inventory.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class ListStockMovementsQueryHandler(
    IStockRepository stock,
    IValidator<ListStockMovementsQuery> validator) : IRequestHandler<ListStockMovementsQuery, Result<PagedStockMovementsResponse>>
{
    public async Task<Result<PagedStockMovementsResponse>> Handle(ListStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedStockMovementsResponse>();
        }

        var movements = await stock.ListMovementsAsync(request.StockItemId, request.Page, request.PageSize, cancellationToken);
        var total = await stock.CountMovementsAsync(request.StockItemId, cancellationToken);

        return Result<PagedStockMovementsResponse>.Success(new PagedStockMovementsResponse(
            movements.Select(movement => movement.ToResponse()).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}

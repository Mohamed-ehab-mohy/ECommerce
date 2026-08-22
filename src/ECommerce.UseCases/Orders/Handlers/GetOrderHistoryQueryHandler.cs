using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Queries;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class GetOrderHistoryQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderHistoryQuery, Result<OrderHistoryResponse>>
{
    public async Task<Result<OrderHistoryResponse>> Handle(
        GetOrderHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var page = await orders.ListByCustomerAsync(
            request.CustomerId,
            request.Cursor,
            request.PageSize,
            cancellationToken);

        var response = new OrderHistoryResponse(
            page.Items
                .Select(order => new OrderHistoryItemResponse(
                    order.Id,
                    order.OrderNumber,
                    order.Status.ToString(),
                    order.GrandTotal,
                    order.Currency,
                    order.PlacedAt,
                    order.Items.Count))
                .ToList(),
            page.NextCursor,
            page.HasNext,
            request.PageSize);

        return Result<OrderHistoryResponse>.Success(response);
    }
}

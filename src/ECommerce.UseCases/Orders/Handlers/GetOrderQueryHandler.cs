using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Queries;
using ECommerce.UseCases.Orders.Responses;
using MediatR;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class GetOrderQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        if (!OrderNumber.TryParse(request.OrderNumber, out var orderNumber) || orderNumber is null)
        {
            return OrderErrors.OrderNotFound;
        }

        var order = await orders.GetByNumberWithDetailsAsync(orderNumber.Value, cancellationToken);

        return order is null
            ? OrderErrors.OrderNotFound
            : request.RequesterCustomerId is { } requesterId
                && order.CustomerId != requesterId
                && !request.SupportAccess
                    ? OrderErrors.NotYourOrder
                    : Result<OrderResponse>.Success(OrderResponse.From(order));
    }
}

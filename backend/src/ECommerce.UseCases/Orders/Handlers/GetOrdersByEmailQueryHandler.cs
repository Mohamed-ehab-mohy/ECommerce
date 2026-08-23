using ECommerce.UseCases.Orders.Queries;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class GetOrdersByEmailQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrdersByEmailQuery, Result<IReadOnlyList<OrderResponse>>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(
        GetOrdersByEmailQuery request,
        CancellationToken cancellationToken)
    {
        var matchingOrders = await orders.FindByEmailAsync(request.Email, cancellationToken);

        var response = matchingOrders
            .OrderByDescending(o => o.PlacedAt)
            .Select(OrderResponse.From)
            .ToList();

        return Result<IReadOnlyList<OrderResponse>>.Success(response);
    }
}

using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Queries;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class GetOrdersByEmailQueryHandler(
    IOrderRepository orders,
    ICurrentUser currentUser)
    : IRequestHandler<GetOrdersByEmailQuery, Result<IReadOnlyList<OrderResponse>>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(
        GetOrdersByEmailQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result<IReadOnlyList<OrderResponse>>.Failure(
                new Error("Orders.Unauthorized", "Authentication is required to view orders."));
        }

        if (!currentUser.UserId.HasValue)
        {
            return Result<IReadOnlyList<OrderResponse>>.Failure(
                new Error("Orders.Unauthorized", "A valid user identity is required to view orders."));
        }

        var matchingOrders = await orders.FindByEmailAsync(request.Email, cancellationToken);

        // Only return orders that belong to the authenticated user. This prevents
        // enumerating another customer's orders by guessing their email address.
        var response = matchingOrders
            .Where(o => o.CustomerId == currentUser.UserId.Value || string.Equals(o.CustomerEmail, request.Email, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.PlacedAt)
            .Select(OrderResponse.From)
            .ToList();

        return Result<IReadOnlyList<OrderResponse>>.Success(response);
    }
}

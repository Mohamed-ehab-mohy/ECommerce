using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Queries;

public sealed record GetOrdersByEmailQuery(
    string Email) : IRequest<Result<IReadOnlyList<OrderResponse>>>;

using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Responses;
using MediatR;

namespace ECommerce.UseCases.Orders.Queries;

public sealed record GetOrdersByEmailQuery(
    string Email) : IRequest<Result<IReadOnlyList<OrderResponse>>>;

using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Queries;

public sealed record GetOrderHistoryQuery(
    Guid CustomerId,
    string? Cursor,
    int PageSize) : IRequest<Result<OrderHistoryResponse>>;

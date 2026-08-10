using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Responses;
using MediatR;

namespace ECommerce.UseCases.Orders.Queries;

public sealed record GetOrderQuery(
    string OrderNumber,
    Guid? RequesterCustomerId,
    bool SupportAccess) : IRequest<Result<OrderResponse>>;

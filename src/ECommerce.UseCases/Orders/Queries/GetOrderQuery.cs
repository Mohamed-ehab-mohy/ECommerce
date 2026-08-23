using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Queries;

public sealed record GetOrderQuery(
    string OrderNumber,
    Guid? RequesterCustomerId,
    bool SupportAccess) : IRequest<Result<OrderResponse>>;

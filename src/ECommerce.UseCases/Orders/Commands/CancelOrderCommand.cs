using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record CancelOrderCommand(
    string OrderNumber,
    string? Reason,
    Guid? RequesterCustomerId,
    bool SupportAccess) : IRequest<Result<OrderResponse>>;

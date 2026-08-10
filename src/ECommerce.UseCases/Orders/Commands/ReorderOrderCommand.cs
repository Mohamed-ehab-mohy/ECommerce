using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;
using MediatR;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record ReorderOrderCommand(
    string OrderNumber,
    Guid? RequesterCustomerId) : IRequest<Result<CartResponse>>;

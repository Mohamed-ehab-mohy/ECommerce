using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record ReorderOrderCommand(
    string OrderNumber,
    Guid? RequesterCustomerId) : IRequest<Result<CartResponse>>;

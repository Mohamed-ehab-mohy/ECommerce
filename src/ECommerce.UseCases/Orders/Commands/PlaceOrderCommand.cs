using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Responses;
using MediatR;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record PlaceOrderCommand(
    Guid CheckoutId,
    string IdempotencyKey) : IRequest<Result<OrderResponse>>;

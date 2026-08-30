using ECommerce.UseCases.Orders.Responses;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record PlaceOrderCommand(
    Guid CheckoutId,
    string IdempotencyKey,
    string CapabilityToken) : IRequest<Result<OrderResponse>>;

using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Cart.Commands;

public sealed record UpdateCartItemCommand(
    string OwnerKey,
    Guid ProductId,
    int Quantity) : IRequest<Result<CartResponse>>;

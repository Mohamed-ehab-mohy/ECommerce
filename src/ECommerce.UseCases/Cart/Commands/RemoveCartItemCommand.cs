using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Cart.Commands;

public sealed record RemoveCartItemCommand(
    string OwnerKey,
    Guid ProductId) : IRequest<Result<CartResponse>>;

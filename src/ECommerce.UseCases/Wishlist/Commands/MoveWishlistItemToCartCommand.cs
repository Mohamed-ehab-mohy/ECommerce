using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Wishlist.Commands;

public sealed record MoveWishlistItemToCartCommand(
    string OwnerKey,
    Guid ProductId,
    string Currency) : IRequest<Result<CartResponse>>;

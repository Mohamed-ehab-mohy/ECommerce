using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Wishlist.Responses;

namespace ECommerce.UseCases.Wishlist.Commands;

public sealed record AddWishlistItemCommand(
    string OwnerKey,
    Guid ProductId) : IRequest<Result<WishlistResponse>>;

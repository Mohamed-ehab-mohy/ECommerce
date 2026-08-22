using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Wishlist.Responses;

namespace ECommerce.UseCases.Wishlist.Queries;

public sealed record GetWishlistQuery(
    string OwnerKey) : IRequest<Result<WishlistResponse>>;

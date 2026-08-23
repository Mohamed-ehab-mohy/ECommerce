using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.UseCases.Wishlist.Responses;

public sealed record WishlistItemResponse(Guid ProductId, DateTime AddedAt);

public sealed record WishlistResponse(
    Guid Id,
    DateTime UpdatedAt,
    IReadOnlyList<WishlistItemResponse> Items);

public static class WishlistResponseFactory
{
    public static WishlistResponse From(WishlistAggregate wishlist) =>
        new(
            wishlist.Id,
            wishlist.UpdatedAt,
            wishlist.Items
                .OrderByDescending(item => item.AddedAt)
                .Select(item => new WishlistItemResponse(item.ProductId, item.AddedAt))
                .ToList());
}

using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Wishlist;

public static class WishlistErrors
{
    public static readonly Error WishlistNotFound = new(
        "Wishlist.WishlistNotFound",
        "The wishlist was not found.",
        ErrorType.NotFound);

    public static readonly Error ItemNotFound = new(
        "Wishlist.ItemNotFound",
        "The item is not on the wishlist.",
        ErrorType.NotFound);

    public static readonly Error ProductInactive = new(
        "Wishlist.ProductInactive",
        "The product is not available.",
        ErrorType.Conflict);

    public static readonly Error ProductOutOfStock = new(
        "Wishlist.ProductOutOfStock",
        "The product is out of stock and cannot be moved to the cart.",
        ErrorType.Conflict);

    public static readonly Error ConcurrencyConflict = new(
        "Wishlist.ConcurrencyConflict",
        "The wishlist was modified concurrently. Reload and retry.",
        ErrorType.Conflict);
}

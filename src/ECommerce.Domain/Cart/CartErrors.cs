using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Cart;

public static class CartErrors
{
    public static readonly Error QuantityOutOfRange = new(
        "Cart.QuantityOutOfRange",
        "Item quantity must be between 1 and 99.",
        ErrorType.Validation);

    public static readonly Error ItemNotFound = new(
        "Cart.ItemNotFound",
        "The item is not in the cart.",
        ErrorType.NotFound);

    public static readonly Error CartNotFound = new(
        "Cart.CartNotFound",
        "The cart was not found.",
        ErrorType.NotFound);

    public static readonly Error ProductInactive = new(
        "Cart.ProductInactive",
        "The product is not available for purchase.",
        ErrorType.Conflict);

    public static readonly Error ConcurrencyConflict = new(
        "Cart.ConcurrencyConflict",
        "The cart was modified concurrently. Reload and retry.",
        ErrorType.Conflict);

    public static readonly Error InvalidPrice = new(
        "Cart.InvalidPrice",
        "The unit price must be non-negative and no greater than the list price.",
        ErrorType.Validation);

    public static readonly Error UnsupportedCurrency = new(
        "Cart.UnsupportedCurrency",
        "The requested currency is not supported.",
        ErrorType.Validation);
}

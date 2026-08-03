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
}

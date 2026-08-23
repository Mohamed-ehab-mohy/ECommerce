namespace ECommerce.Domain.Cart;

public sealed class CartConcurrencyException : Exception
{
    public CartConcurrencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

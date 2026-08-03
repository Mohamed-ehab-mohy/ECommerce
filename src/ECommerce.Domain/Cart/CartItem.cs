namespace ECommerce.Domain.Cart;

public sealed class CartItem
{
    private CartItem()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public Guid CartId { get; internal set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public string? ImageUrl { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static CartItem Create(
        Guid productId,
        string sku,
        string name,
        decimal unitPrice,
        int quantity,
        string? imageUrl,
        DateTime utcNow)
    {
        return new CartItem
        {
            ProductId = productId,
            Sku = sku,
            Name = name,
            UnitPrice = unitPrice,
            Quantity = quantity,
            ImageUrl = imageUrl,
            UpdatedAt = utcNow
        };
    }

    public static CartItem Rehydrate(
        Guid productId,
        string sku,
        string name,
        decimal unitPrice,
        int quantity,
        string? imageUrl,
        DateTime updatedAt)
    {
        return new CartItem
        {
            ProductId = productId,
            Sku = sku,
            Name = name,
            UnitPrice = unitPrice,
            Quantity = quantity,
            ImageUrl = imageUrl,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateQuantity(int quantity, DateTime utcNow)
    {
        Quantity = quantity;
        UpdatedAt = utcNow;
    }
}

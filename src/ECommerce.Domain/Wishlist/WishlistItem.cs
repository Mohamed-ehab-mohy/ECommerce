namespace ECommerce.Domain.Wishlist;

public sealed class WishlistItem
{
    private WishlistItem()
    {
    }

    public Guid WishlistId { get; internal set; }

    public Guid ProductId { get; private set; }

    public DateTime AddedAt { get; private set; }

    public static WishlistItem Create(Guid productId, DateTime utcNow) =>
        new()
        {
            ProductId = productId,
            AddedAt = utcNow
        };

    public static WishlistItem Rehydrate(Guid productId, DateTime addedAt) =>
        new()
        {
            ProductId = productId,
            AddedAt = addedAt
        };
}

using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Wishlist;

public sealed class Wishlist : BaseEntity<Guid>
{
    private readonly List<WishlistItem> _items = [];

    private Wishlist()
    {
        OwnerKey = string.Empty;
    }

    public string OwnerKey { get; private set; }

    public IReadOnlyCollection<WishlistItem> Items => _items;

    public static Wishlist Create(string ownerKey, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerKey,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

    public static Wishlist Rehydrate(
        Guid id,
        string ownerKey,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        IEnumerable<WishlistItem> items)
    {
        var wishlist = new Wishlist
        {
            Id = id,
            OwnerKey = ownerKey,
            CreatedAt = createdAtUtc,
            UpdatedAt = updatedAtUtc
        };

        foreach (var item in items)
        {
            item.WishlistId = id;
            wishlist._items.Add(item);
        }

        return wishlist;
    }

    public Result AddItem(Guid productId, DateTime utcNow)
    {
        if (_items.Any(item => item.ProductId == productId))
        {
            return Result.Success();
        }

        var item = WishlistItem.Create(productId, utcNow);
        item.WishlistId = Id;
        _items.Add(item);

        Touch(utcNow);
        AddDomainEvent(new WishlistItemAdded(Id, productId));

        return Result.Success();
    }

    public Result RemoveItem(Guid productId, DateTime utcNow)
    {
        var item = _items.FirstOrDefault(item => item.ProductId == productId);
        if (item is null)
        {
            return WishlistErrors.ItemNotFound;
        }

        _items.Remove(item);
        Touch(utcNow);
        AddDomainEvent(new WishlistItemRemoved(Id, productId));

        return Result.Success();
    }

    private void Touch(DateTime utcNow) => UpdatedAt = utcNow;
}

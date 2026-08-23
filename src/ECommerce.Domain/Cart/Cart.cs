using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Cart;

public sealed class Cart : BaseEntity<Guid>
{
    private readonly List<CartItem> _items = [];

    private Cart()
    {
        OwnerKey = string.Empty;
        Currency = string.Empty;
    }

    public string OwnerKey { get; private set; }

    public string Currency { get; private set; }

    public long Version { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public string? AppliedCouponCode { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items;

    public void SetVersion(long version) => Version = version;

    public static Cart Create(string ownerKey, string currency, DateTime expiresAtUtc, DateTime utcNow)
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerKey,
            Currency = currency,
            Version = 1,
            ExpiresAt = expiresAtUtc,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public static Cart Rehydrate(
        Guid id,
        string ownerKey,
        string currency,
        long version,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        IEnumerable<CartItem> items,
        string? appliedCouponCode = null)
    {
        var cart = new Cart
        {
            Id = id,
            OwnerKey = ownerKey,
            Currency = currency,
            Version = version,
            ExpiresAt = expiresAtUtc,
            CreatedAt = createdAtUtc,
            UpdatedAt = updatedAtUtc,
            AppliedCouponCode = appliedCouponCode
        };

        foreach (var item in items)
        {
            item.CartId = id;
            cart._items.Add(item);
        }

        return cart;
    }

    public Result AddItem(
        Guid productId,
        string sku,
        string name,
        decimal listPrice,
        decimal? offerPrice,
        int quantity,
        string? imageUrl,
        DateTime utcNow)
    {
        var unitPrice = offerPrice ?? listPrice;

        return AddItem(productId, sku, name, listPrice, unitPrice, quantity, imageUrl, utcNow);
    }

    public Result AddItem(
        Guid productId,
        string sku,
        string name,
        decimal listPrice,
        decimal unitPrice,
        int quantity,
        string? imageUrl,
        DateTime utcNow)
    {
        if (quantity is < 1 or > 99)
        {
            return CartErrors.QuantityOutOfRange;
        }

        if (unitPrice > listPrice || unitPrice < 0m)
        {
            return CartErrors.InvalidPrice;
        }

        var existing = _items.FirstOrDefault(item => item.ProductId == productId);
        if (existing is null)
        {
            var item = CartItem.Create(productId, sku, name, listPrice, unitPrice, quantity, imageUrl, utcNow);
            item.CartId = Id;
            _items.Add(item);
        }
        else
        {
            var updatedQuantity = existing.Quantity + quantity;
            if (updatedQuantity > 99)
            {
                return CartErrors.QuantityOutOfRange;
            }

            existing.UpdateQuantity(updatedQuantity, utcNow);
        }

        Touch(utcNow);

        AddDomainEvent(new CartItemAdded(Id, productId, quantity));

        return Result.Success();
    }

    public Result UpdateQuantity(Guid productId, int quantity, DateTime utcNow)
    {
        if (quantity is < 1 or > 99)
        {
            return CartErrors.QuantityOutOfRange;
        }

        var item = _items.FirstOrDefault(item => item.ProductId == productId);
        if (item is null)
        {
            return CartErrors.ItemNotFound;
        }

        item.UpdateQuantity(quantity, utcNow);
        Touch(utcNow);

        return Result.Success();
    }

    public Result RemoveItem(Guid productId, DateTime utcNow)
    {
        var item = _items.FirstOrDefault(item => item.ProductId == productId);
        if (item is null)
        {
            return CartErrors.ItemNotFound;
        }

        _items.Remove(item);
        Touch(utcNow);

        AddDomainEvent(new CartItemRemoved(Id, productId));

        return Result.Success();
    }

    public void MergeFrom(Cart other, DateTime utcNow)
    {
        var mergedItems = 0;

        foreach (var incoming in other.Items)
        {
            var existing = _items.FirstOrDefault(item => item.ProductId == incoming.ProductId);
            if (existing is null)
            {
                incoming.CartId = Id;
                _items.Add(incoming);
                mergedItems++;
                continue;
            }

            if (incoming.UpdatedAt > existing.UpdatedAt)
            {
                existing.UpdateQuantity(incoming.Quantity, utcNow);
            }
        }

        other.Clear(utcNow);
        Touch(utcNow);

        AddDomainEvent(new CartMerged(Id, other.Id, mergedItems));
    }

    public void Clear(DateTime utcNow)
    {
        _items.Clear();
        Touch(utcNow);
    }

    public void ApplyCoupon(string code, DateTime utcNow)
    {
        if (AppliedCouponCode == code.Trim().ToUpperInvariant())
        {
            return;
        }

        AppliedCouponCode = code.Trim().ToUpperInvariant();
        Touch(utcNow);

        AddDomainEvent(new CartCouponApplied(Id, AppliedCouponCode));
    }

    public void RemoveCoupon(DateTime utcNow)
    {
        if (AppliedCouponCode is null)
        {
            return;
        }

        AppliedCouponCode = null;
        Touch(utcNow);

        AddDomainEvent(new CartCouponRemoved(Id));
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    private void Touch(DateTime utcNow) => UpdatedAt = utcNow;
}

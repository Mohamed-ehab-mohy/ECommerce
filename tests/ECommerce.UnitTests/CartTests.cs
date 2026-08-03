using ECommerce.Domain.Cart;
using ECommerce.Domain.Events;

namespace ECommerce.UnitTests;

public sealed class CartTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    private static Cart CreateCart(string ownerKey = "anon-123", string currency = "USD") =>
        Cart.Create(ownerKey, currency, UtcNow.AddDays(30), UtcNow);

    [Fact]
    public void Create_Sets_Owner_Currency_Version_And_Expiry()
    {
        var cart = CreateCart();

        Assert.Equal("anon-123", cart.OwnerKey);
        Assert.Equal("USD", cart.Currency);
        Assert.Equal(1, cart.Version);
        Assert.Equal(UtcNow.AddDays(30), cart.ExpiresAt);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_Adds_Line_And_Raises_Event()
    {
        var cart = CreateCart();
        var result = cart.AddItem(Guid.NewGuid(), "SKU-1", "Widget", 10.00m, 2, null, UtcNow);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(cart.Items);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(2, item.Quantity);
        Assert.IsType<CartItemAdded>(Assert.Single(cart.DomainEvents));
    }

    [Fact]
    public void AddItem_Same_Product_Merges_Quantity()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();

        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 2, null, UtcNow);
        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 3, null, UtcNow);

        Assert.Single(cart.Items);
        Assert.Equal(5, Assert.Single(cart.Items).Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void AddItem_Rejects_Quantity_Outside_Bounds(int quantity)
    {
        var cart = CreateCart();
        var result = cart.AddItem(Guid.NewGuid(), "SKU-1", "Widget", 10.00m, quantity, null, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.QuantityOutOfRange, result.Error);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_Rejects_When_Combined_Quantity_Exceeds_99()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();

        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 95, null, UtcNow);
        var result = cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 10, null, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.QuantityOutOfRange, result.Error);
        Assert.Equal(95, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void UpdateQuantity_Updates_Existing_Line()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 2, null, UtcNow);

        var result = cart.UpdateQuantity(productId, 7, UtcNow.AddSeconds(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void UpdateQuantity_Returns_ItemNotFound_For_Missing_Line()
    {
        var cart = CreateCart();
        var result = cart.UpdateQuantity(Guid.NewGuid(), 7, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound, result.Error);
    }

    [Fact]
    public void RemoveItem_Removes_Line_And_Raises_Event()
    {
        var cart = CreateCart();
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 2, null, UtcNow);

        var result = cart.RemoveItem(productId, UtcNow.AddSeconds(1));

        Assert.True(result.IsSuccess);
        Assert.Empty(cart.Items);
        Assert.IsType<CartItemRemoved>(cart.DomainEvents.Last());
    }

    [Fact]
    public void RemoveItem_Returns_ItemNotFound_For_Missing_Line()
    {
        var cart = CreateCart();
        var result = cart.RemoveItem(Guid.NewGuid(), UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound, result.Error);
    }

    [Fact]
    public void Clear_Empties_Items()
    {
        var cart = CreateCart();
        cart.AddItem(Guid.NewGuid(), "SKU-1", "Widget", 10.00m, 2, null, UtcNow);
        cart.AddItem(Guid.NewGuid(), "SKU-2", "Gadget", 20.00m, 1, null, UtcNow);

        cart.Clear(UtcNow.AddSeconds(1));

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void MergeFrom_Adds_Missing_Items_And_Raises_Event()
    {
        var cart = CreateCart("user-1");
        var guest = CreateCart("anon-999");
        var productId = Guid.NewGuid();
        guest.AddItem(productId, "SKU-1", "Widget", 10.00m, 3, null, UtcNow);

        cart.MergeFrom(guest, UtcNow.AddSeconds(1));

        Assert.Single(cart.Items);
        Assert.Equal(3, Assert.Single(cart.Items).Quantity);
        Assert.Empty(guest.Items);
        Assert.IsType<CartMerged>(Assert.Single(cart.DomainEvents));
    }

    [Fact]
    public void MergeFrom_Keeps_Newer_Line_On_Conflict()
    {
        var cart = CreateCart("user-1");
        var guest = CreateCart("anon-999");
        var productId = Guid.NewGuid();

        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 1, null, UtcNow.AddSeconds(-10));
        guest.AddItem(productId, "SKU-1", "Widget", 10.00m, 5, null, UtcNow);

        cart.MergeFrom(guest, UtcNow);

        Assert.Equal(5, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void IsExpired_Returns_True_After_Expiry()
    {
        var cart = Cart.Create("anon-123", "USD", UtcNow, UtcNow);
        Assert.True(cart.IsExpired(UtcNow.AddSeconds(1)));
        Assert.False(cart.IsExpired(UtcNow.AddSeconds(-1)));
    }

    [Fact]
    public void Rehydrate_Restores_State()
    {
        var productId = Guid.NewGuid();
        var cart = Cart.Create("anon-123", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(productId, "SKU-1", "Widget", 10.00m, 2, null, UtcNow);

        var restored = Cart.Rehydrate(
            cart.Id,
            cart.OwnerKey,
            cart.Currency,
            cart.Version,
            cart.ExpiresAt,
            cart.CreatedAt,
            cart.UpdatedAt,
            cart.Items);

        Assert.Equal(cart.Id, restored.Id);
        Assert.Equal(cart.OwnerKey, restored.OwnerKey);
        Assert.Equal(cart.Version, restored.Version);
        var item = Assert.Single(restored.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }
}

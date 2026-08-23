using ECommerce.Domain.Events;
using ECommerce.Domain.Wishlist;

namespace ECommerce.UnitTests;

public sealed class WishlistTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Starts_Empty_And_Owned()
    {
        var wishlist = Wishlist.Create("user:1", UtcNow);

        Assert.Equal("user:1", wishlist.OwnerKey);
        Assert.Empty(wishlist.Items);
    }

    [Fact]
    public void AddItem_Appends_And_Emits_Event()
    {
        var wishlist = Wishlist.Create("user:1", UtcNow);
        var productId = Guid.NewGuid();

        var result = wishlist.AddItem(productId, UtcNow);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(wishlist.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Contains(wishlist.DomainEvents, e => e is WishlistItemAdded added && added.ProductId == productId);
    }

    [Fact]
    public void AddItem_Is_Idempotent()
    {
        var wishlist = Wishlist.Create("user:1", UtcNow);
        var productId = Guid.NewGuid();

        wishlist.AddItem(productId, UtcNow);
        var result = wishlist.AddItem(productId, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Single(wishlist.Items);
    }

    [Fact]
    public void RemoveItem_Removes_And_Emits_Event()
    {
        var wishlist = Wishlist.Create("user:1", UtcNow);
        var productId = Guid.NewGuid();
        wishlist.AddItem(productId, UtcNow);

        var result = wishlist.RemoveItem(productId, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Empty(wishlist.Items);
        Assert.Contains(wishlist.DomainEvents, e => e is WishlistItemRemoved removed && removed.ProductId == productId);
    }

    [Fact]
    public void RemoveItem_Unknown_Returns_ItemNotFound()
    {
        var wishlist = Wishlist.Create("user:1", UtcNow);

        var result = wishlist.RemoveItem(Guid.NewGuid(), UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(WishlistErrors.ItemNotFound, result.Error);
    }
}

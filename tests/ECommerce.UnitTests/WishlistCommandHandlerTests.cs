using ECommerce.Domain.Catalog;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Wishlist;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Pricing;
using ECommerce.UseCases.Wishlist.Commands;
using ECommerce.UseCases.Wishlist.Handlers;
using ECommerce.UseCases.Wishlist.Queries;
using Microsoft.Extensions.Logging.Abstractions;
using WishlistAggregate = ECommerce.Domain.Wishlist.Wishlist;

namespace ECommerce.UnitTests;

public sealed class WishlistCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 15, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeWishlistRepository _wishlists = new();

    private readonly FakeCartRepository _carts = new();

    private readonly FakeProductRepository _products = new();

    private readonly FakeStockRepository _stock = new();

    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private AddWishlistItemCommandHandler AddHandler =>
        new(
            _wishlists,
            _products,
            new FixedTimeProvider(UtcNow),
            new AddWishlistItemCommandValidator());

    private RemoveWishlistItemCommandHandler RemoveHandler =>
        new(
            _wishlists,
            new FixedTimeProvider(UtcNow),
            new RemoveWishlistItemCommandValidator());

    private MoveWishlistItemToCartCommandHandler MoveHandler =>
        new(
            _wishlists,
            _carts,
            _products,
            _stock,
            _currencies,
            new FixedTimeProvider(UtcNow),
            new MoveWishlistItemToCartCommandValidator(_currencies));

    private GetWishlistQueryHandler GetHandler =>
        new(
            _wishlists,
            new FixedTimeProvider(UtcNow),
            new GetWishlistQueryValidator());

    private Product CreateActiveProduct(string sku = "SKU-1", string name = "Widget") =>
        Product.Create(
            sku, "widget", "en", name, null,
            "USD", 20.00m, 15.00m, null, null, false, ProductStatus.Active, UtcNow);

    private void SeedStock(string sku, int onHand)
    {
        var item = StockItem.Create(sku, WarehouseId, UtcNow);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, onHand, "RECV", null, null, UtcNow), UtcNow);
        _stock.Items.Add(item);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Get_Returns_Empty_Wishlist_When_None()
    {
        var result = await GetHandler.Handle(new GetWishlistQuery("user:1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Add_Creates_Wishlist_And_Item()
    {
        var product = CreateActiveProduct();
        _products.Add(product);

        var result = await AddHandler.Handle(
            new AddWishlistItemCommand("user:1", product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var wishlist = Assert.Single(_wishlists.Wishlists);
        Assert.Equal("user:1", wishlist.OwnerKey);
        Assert.Equal(product.Id, Assert.Single(wishlist.Items).ProductId);
        Assert.Equal(product.Id, Assert.Single(result.Value.Items).ProductId);
    }

    [Fact]
    public async Task Add_Unknown_Product_Returns_NotFound()
    {
        var result = await AddHandler.Handle(
            new AddWishlistItemCommand("user:1", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task Add_Inactive_Product_Returns_Conflict()
    {
        var product = Product.Create(
            "SKU-1", "widget", "en", "Widget", null,
            "USD", 20.00m, null, null, null, false, ProductStatus.Inactive, UtcNow);
        _products.Add(product);

        var result = await AddHandler.Handle(
            new AddWishlistItemCommand("user:1", product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WishlistErrors.ProductInactive, result.Error);
        Assert.Empty(_wishlists.Wishlists);
    }

    [Fact]
    public async Task Add_Same_Product_Twice_Is_Idempotent()
    {
        var product = CreateActiveProduct();
        _products.Add(product);

        await AddHandler.Handle(new AddWishlistItemCommand("user:1", product.Id), CancellationToken.None);
        await AddHandler.Handle(new AddWishlistItemCommand("user:1", product.Id), CancellationToken.None);

        var wishlist = Assert.Single(_wishlists.Wishlists);
        Assert.Single(wishlist.Items);
    }

    [Fact]
    public async Task Remove_Deletes_Item()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        var wishlist = WishlistAggregate.Create("user:1", UtcNow);
        wishlist.AddItem(product.Id, UtcNow);
        _wishlists.Wishlists.Add(wishlist);

        var result = await RemoveHandler.Handle(
            new RemoveWishlistItemCommand("user:1", product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_wishlists.Wishlists[0].Items);
    }

    [Fact]
    public async Task Remove_Unknown_Wishlist_Returns_NotFound()
    {
        var result = await RemoveHandler.Handle(
            new RemoveWishlistItemCommand("user:1", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WishlistErrors.WishlistNotFound, result.Error);
    }

    [Fact]
    public async Task Move_Adds_To_Cart_And_Removes_From_Wishlist()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        SeedStock("SKU-1", 10);
        var wishlist = WishlistAggregate.Create("user:1", UtcNow);
        wishlist.AddItem(product.Id, UtcNow);
        _wishlists.Wishlists.Add(wishlist);

        var result = await MoveHandler.Handle(
            new MoveWishlistItemToCartCommand("user:1", product.Id, "USD"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Empty(_wishlists.Wishlists[0].Items);
        var cart = Assert.Single(_carts.Carts);
        var item = Assert.Single(cart.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(1, item.Quantity);
        var moved = Assert.Single(result.Value.Items);
        Assert.Equal(product.Id, moved.ProductId);
    }

    [Fact]
    public async Task Move_Out_Of_Stock_Returns_Conflict()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        var wishlist = WishlistAggregate.Create("user:1", UtcNow);
        wishlist.AddItem(product.Id, UtcNow);
        _wishlists.Wishlists.Add(wishlist);

        var result = await MoveHandler.Handle(
            new MoveWishlistItemToCartCommand("user:1", product.Id, "USD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WishlistErrors.ProductOutOfStock, result.Error);
        Assert.Single(_wishlists.Wishlists[0].Items);
        Assert.Empty(_carts.Carts);
    }

    [Fact]
    public async Task Move_Item_Not_On_Wishlist_Returns_ItemNotFound()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        SeedStock("SKU-1", 10);
        _wishlists.Wishlists.Add(WishlistAggregate.Create("user:1", UtcNow));

        var result = await MoveHandler.Handle(
            new MoveWishlistItemToCartCommand("user:1", product.Id, "USD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WishlistErrors.ItemNotFound, result.Error);
    }

    [Fact]
    public async Task Move_Unknown_Wishlist_Returns_ItemNotFound()
    {
        var product = CreateActiveProduct();
        _products.Add(product);
        SeedStock("SKU-1", 10);

        var result = await MoveHandler.Handle(
            new MoveWishlistItemToCartCommand("user:1", product.Id, "USD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WishlistErrors.ItemNotFound, result.Error);
    }
}

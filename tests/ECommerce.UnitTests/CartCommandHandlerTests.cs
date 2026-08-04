using ECommerce.Domain.Cart;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Handlers;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging.Abstractions;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UnitTests;

public sealed class CartCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeCartRepository _carts = new();

    private readonly FakeProductRepository _products = new();

    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private AddCartItemCommandHandler AddHandler => new(
        _carts,
        _products,
        _currencies,
        TimeProvider.System,
        new AddCartItemCommandValidator(_currencies),
        NullLogger<AddCartItemCommandHandler>.Instance);

    private UpdateCartItemCommandHandler UpdateHandler => new(
        _carts,
        _currencies,
        TimeProvider.System,
        new UpdateCartItemCommandValidator(),
        NullLogger<UpdateCartItemCommandHandler>.Instance);

    private RemoveCartItemCommandHandler RemoveHandler => new(
        _carts,
        _currencies,
        TimeProvider.System,
        new RemoveCartItemCommandValidator(),
        NullLogger<RemoveCartItemCommandHandler>.Instance);

    [Fact]
    public async Task AddItem_Creates_Cart_And_Snapshots_Price()
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        _products.Products.Add(product);

        var result = await AddHandler.Handle(
            new AddCartItemCommand("anon-1", "USD", product.Id, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal(1, result.Value.Version);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(20.00m, item.ListPrice);
        Assert.Equal(15.00m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(40.00m, item.LineSubtotal);
        Assert.Equal(10.00m, item.LineDiscount);
        Assert.Equal(9.90m, result.Value.Totals.Shipping);
    }

    [Fact]
    public async Task AddItem_Inactive_Product_Returns_409_Conflict()
    {
        var product = Product.Create(
            "SKU-1", "widget", "en", "Widget", null,
            "USD", 20.00m, null, null, null, false, ProductStatus.Inactive, UtcNow);
        _products.Products.Add(product);

        var result = await AddHandler.Handle(
            new AddCartItemCommand("anon-1", "USD", product.Id, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ProductInactive, result.Error);
        Assert.Empty(_carts.Carts);
    }

    [Fact]
    public async Task AddItem_Unknown_Product_Returns_NotFound()
    {
        var result = await AddHandler.Handle(
            new AddCartItemCommand("anon-1", "USD", Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.ProductNotFound, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task AddItem_Rejects_Quantity_Outside_Bounds(int quantity)
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        _products.Products.Add(product);

        var result = await AddHandler.Handle(
            new AddCartItemCommand("anon-1", "USD", product.Id, quantity),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.Empty(_carts.Carts);
    }

    [Fact]
    public async Task AddItem_Merges_Quantity_For_Existing_Line()
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        _products.Products.Add(product);
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 1, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await AddHandler.Handle(
            new AddCartItemCommand("anon-1", "USD", product.Id, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, Assert.Single(result.Value.Items).Quantity);
    }

    [Fact]
    public async Task UpdateQuantity_Sets_New_Quantity()
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 2, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await UpdateHandler.Handle(
            new UpdateCartItemCommand("anon-1", product.Id, 5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, Assert.Single(result.Value.Items).Quantity);
    }

    [Fact]
    public async Task UpdateQuantity_Zero_Removes_Line()
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 2, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await UpdateHandler.Handle(
            new UpdateCartItemCommand("anon-1", product.Id, 0),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task UpdateQuantity_On_Missing_Cart_Returns_NotFound()
    {
        var result = await UpdateHandler.Handle(
            new UpdateCartItemCommand("anon-1", Guid.NewGuid(), 3),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.CartNotFound, result.Error);
    }

    [Fact]
    public async Task RemoveItem_Removes_Line()
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 2, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await RemoveHandler.Handle(
            new RemoveCartItemCommand("anon-1", product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task RemoveItem_On_Missing_Line_Returns_NotFound()
    {
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        _carts.Carts.Add(cart);

        var result = await RemoveHandler.Handle(
            new RemoveCartItemCommand("anon-1", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound, result.Error);
    }

    [Fact]
    public async Task Save_Concurrency_Violation_Returns_Conflict()
    {
        var product = CreateActiveProduct("SKU-1", "Widget", list: 20.00m, offer: 15.00m);
        _products.Products.Add(product);
        _carts.ThrowConcurrencyOnSave = true;

        var result = await AddHandler.Handle(
            new AddCartItemCommand("anon-1", "USD", product.Id, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ConcurrencyConflict, result.Error);
    }

    private static Product CreateActiveProduct(string sku, string name, decimal list, decimal? offer) =>
        Product.Create(
            sku, sku.ToLowerInvariant().Replace(" ", "-"), "en", name, null,
            "USD", list, offer, null, null, false, ProductStatus.Active, UtcNow);
}

using ECommerce.Domain.Cart;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Cart.Handlers;
using ECommerce.UseCases.Cart.Queries;
using ECommerce.UseCases.Pricing;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UnitTests;

public sealed class GetCartPriceChangesQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeCartRepository _carts = new();

    private readonly FakeProductRepository _products = new();

    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private GetCartPriceChangesQueryHandler Handler => new(_carts, _products, _currencies);

    [Fact]
    public async Task Returns_Empty_When_Cart_Does_Not_Exist()
    {
        var result = await Handler.Handle(new GetCartPriceChangesQuery("anon-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Warnings);
    }

    [Fact]
    public async Task No_Warnings_When_Prices_Are_Unchanged()
    {
        var product = CreateProduct("SKU-1", list: 20.00m, offer: 15.00m);
        _products.Products.Add(product);

        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(product.Id, "SKU-1", "Widget", 20.00m, 15.00m, 2, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await Handler.Handle(new GetCartPriceChangesQuery("anon-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Warnings);
    }

    [Fact]
    public async Task Warns_When_Current_Price_Differs_From_Cart_Snapshot()
    {
        var product = CreateProduct("SKU-1", list: 25.00m, offer: null);
        _products.Products.Add(product);

        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(product.Id, "SKU-1", "Widget", 20.00m, 15.00m, 2, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await Handler.Handle(new GetCartPriceChangesQuery("anon-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var warning = Assert.Single(result.Value.Warnings);
        Assert.Equal(product.Id, warning.ProductId);
        Assert.Equal("SKU-1", warning.Sku);
        Assert.Equal(15.00m, warning.CartUnitPrice);
        Assert.Equal(25.00m, warning.CurrentUnitPrice);
        Assert.Equal(10.00m, warning.Delta);
    }

    [Fact]
    public async Task Skips_Items_Whose_Product_Is_Missing_From_Catalog()
    {
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 1, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await Handler.Handle(new GetCartPriceChangesQuery("anon-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Warnings);
    }

    private static Product CreateProduct(string sku, decimal list, decimal? offer) =>
        Product.Create(
            sku, "widget", "en", "Widget", null,
            "USD", list, offer, null, null, false, ProductStatus.Active, UtcNow);
}

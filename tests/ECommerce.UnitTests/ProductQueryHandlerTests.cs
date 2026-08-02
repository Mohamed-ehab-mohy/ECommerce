using ECommerce.Domain.Catalog;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Catalog.Queries;

namespace ECommerce.UnitTests;

public sealed class ProductQueryHandlerTests
{
    private readonly FakeProductRepository _products = new();

    private GetProductQueryHandler GetHandler => new(_products);

    private ListProductsQueryHandler ListHandler => new(_products, new ListProductsQueryValidator());

    [Fact]
    public async Task GetProduct_Returns_Active_Product()
    {
        var product = CreateProduct("SKU-001", "wireless-headphones", name: "Wireless Headphones");
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "en", "USD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SKU-001", result.Value.Sku);
        Assert.Equal("Wireless Headphones", result.Value.Name);
        Assert.Equal(100m, result.Value.ListAmount);
        Assert.Equal("USD", result.Value.Currency);
    }

    [Fact]
    public async Task GetProduct_With_Unknown_Id_Returns_NotFound()
    {
        var result = await GetHandler.Handle(new GetProductQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.ProductNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetProduct_Deactivated_Product_Returns_NotFound()
    {
        var product = CreateProduct("SKU-002", "hidden");
        product.Deactivate();
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task ListProducts_Returns_Only_Active_Products()
    {
        var active = CreateProduct("SKU-003", "active", name: "Active");
        var draft = CreateProduct("SKU-004", "draft", status: ProductStatus.Draft);
        var inactive = CreateProduct("SKU-005", "inactive");
        inactive.Deactivate();

        _products.Products.Add(active);
        _products.Products.Add(draft);
        _products.Products.Add(inactive);

        var result = await ListHandler.Handle(new ListProductsQuery(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("active", item.Slug);
    }

    [Fact]
    public async Task ListProducts_With_Invalid_Page_Returns_Validation_Failure()
    {
        var result = await ListHandler.Handle(new ListProductsQuery(0, 20), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    private static Product CreateProduct(
        string sku,
        string slug,
        string name = "Test Product",
        ProductStatus status = ProductStatus.Active) =>
        Product.Create(
            sku,
            slug,
            "en",
            name,
            null,
            "USD",
            100m,
            null,
            null,
            null,
            false,
            status,
            DateTime.UtcNow);
}

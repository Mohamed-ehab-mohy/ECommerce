using ECommerce.Domain.Catalog;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UnitTests;

public sealed class ProductQueryHandlerTests
{
    private readonly FakeProductRepository _products = new();

    private readonly ILocaleCatalog _locales = new DefaultLocaleCatalog();

    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private GetProductQueryHandler GetHandler =>
        new(_products, _locales, _currencies, new GetProductQueryValidator(_currencies, _locales));

    private ListProductsQueryHandler ListHandler => new(_products, _locales, _currencies, new ListProductsQueryValidator(_currencies, _locales));

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

    [Fact]
    public async Task ListProducts_With_Unsupported_Currency_Returns_Validation_Failure()
    {
        var result = await ListHandler.Handle(new ListProductsQuery(1, 20, Currency: "JPY"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task GetProduct_Converts_Price_To_Requested_Currency()
    {
        var product = CreateProduct("SKU-006", "convertible", name: "Convertible");
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "en", "AED"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("AED", result.Value.Currency);
        Assert.Equal(367.25m, result.Value.ListAmount);
    }

    [Fact]
    public async Task GetProduct_Converts_List_And_Offer_Amounts()
    {
        var product = CreateProduct("SKU-007", "with-offer");
        product.UpdateDetails(null, null, null, null, null, null, null, null, "USD", 100m, 80m, DateTime.UtcNow);
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "en", "EGP"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EGP", result.Value.Currency);
        Assert.Equal(4850m, result.Value.ListAmount);
        Assert.Equal(3880m, result.Value.OfferAmount);
    }

    [Fact]
    public async Task GetProduct_Falls_Back_To_Default_Locale_When_Requested_Translation_Missing()
    {
        var product = CreateProduct("SKU-008", "localized", name: "English Name");
        product.UpdateDetails(null, null, null, null, null, "ar", "Arabic Name", null, null, null, null, DateTime.UtcNow);
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "fr", "USD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("English Name", result.Value.Name);
    }

    [Fact]
    public async Task GetProduct_Falls_Back_To_First_Translation_When_Default_Locale_Also_Missing()
    {
        var product = Product.Create(
            "SKU-009",
            "arabic-only",
            "ar",
            "اسم عربي",
            null,
            "USD",
            100m,
            null,
            null,
            null,
            false,
            ProductStatus.Active,
            DateTime.UtcNow);
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "fr", "USD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("اسم عربي", result.Value.Name);
    }

    [Fact]
    public async Task GetProduct_Returns_Localized_Name_For_Requested_Locale()
    {
        var product = CreateProduct("SKU-010", "multi-translation", name: "Wireless Headphones");
        product.UpdateDetails(null, null, null, null, null, "ar", "سماعات لاسلكية", null, null, null, null, DateTime.UtcNow);
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "ar", "USD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("سماعات لاسلكية", result.Value.Name);
    }

    [Fact]
    public async Task GetProduct_Returns_Exact_Price_When_Requested_Currency_Has_Own_Price()
    {
        var product = CreateProduct("SKU-011", "multi-currency", name: "Wireless Headphones");
        product.UpdateDetails(null, null, null, null, null, null, null, null, "EUR", 92m, null, DateTime.UtcNow);
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "en", "EUR"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR", result.Value.Currency);
        Assert.Equal(92m, result.Value.ListAmount);
    }

    [Fact]
    public async Task GetProduct_Falls_Back_To_Default_Translation_And_Converts_Price()
    {
        var product = CreateProduct("SKU-012", "localized-priced", name: "Wireless Headphones");
        product.UpdateDetails(null, null, null, null, null, "ar", "سماعات لاسلكية", null, null, null, null, DateTime.UtcNow);
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "fr", "AED"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Wireless Headphones", result.Value.Name);
        Assert.Equal("AED", result.Value.Currency);
        Assert.Equal(367.25m, result.Value.ListAmount);
    }

    [Fact]
    public async Task GetProduct_With_Unsupported_Currency_Returns_Validation_Failure()
    {
        var product = CreateProduct("SKU-013", "invalid-currency");
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "en", "JPY"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task GetProduct_With_Unsupported_Locale_Returns_Validation_Failure()
    {
        var product = CreateProduct("SKU-014", "invalid-locale");
        _products.Products.Add(product);

        var result = await GetHandler.Handle(new GetProductQuery(product.Id, "xx", "USD"), CancellationToken.None);

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

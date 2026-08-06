using ECommerce.Domain.Catalog;
using ECommerce.Domain.Events;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UnitTests;

public sealed class ProductCommandHandlerTests
{
    private readonly FakeProductRepository _products = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();
    private readonly FakeAuditContextProvider _auditContext = new();
    private readonly ILocaleCatalog _locales = new DefaultLocaleCatalog();
    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private CreateProductCommandHandler CreateHandler =>
        new(_products, _unitOfWork, _timeProvider, new CreateProductCommandValidator(_currencies, _locales),
            new AuditLogWriter(_auditEntries, _auditContext));

    private UpdateProductCommandHandler UpdateHandler =>
        new(_products, _unitOfWork, _timeProvider, new UpdateProductCommandValidator(_currencies, _locales),
            new AuditLogWriter(_auditEntries, _auditContext));

    private DeactivateProductCommandHandler DeactivateHandler =>
        new(_products, _unitOfWork, new AuditLogWriter(_auditEntries, _auditContext));

    [Fact]
    public async Task CreateProduct_Adds_Product_With_Translation_Price_And_Event()
    {
        var result = await CreateHandler.Handle(
            new CreateProductCommand(
                "SKU-100",
                "wireless-headphones",
                "Wireless Headphones",
                "Premium audio",
                "USD",
                349.00m,
                299.00m,
                null,
                null,
                false,
                "Draft",
                "en"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var product = Assert.Single(_products.Products);
        Assert.Equal("SKU-100", product.Sku);
        Assert.Equal("wireless-headphones", product.Slug);
        Assert.Equal("Wireless Headphones", Assert.Single(product.Translations).Name);
        Assert.Equal(349.00m, Assert.Single(product.Prices).ListAmount);
        Assert.Equal(299.00m, Assert.Single(product.Prices).OfferAmount);
        Assert.Equal(1, _unitOfWork.SaveCount);

        Assert.IsType<ProductCreated>(Assert.Single(product.DomainEvents));
    }

    [Fact]
    public async Task CreateProduct_With_Duplicate_Sku_Returns_Conflict()
    {
        _products.Products.Add(CreateProduct("SKU-100", "first"));

        var result = await CreateHandler.Handle(
            new CreateProductCommand(
                "SKU-100",
                "second-slug",
                "Another",
                null,
                "USD",
                10m,
                null,
                null,
                null,
                false,
                "Active",
                "en"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.SkuAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProduct_With_Duplicate_Slug_Returns_Conflict()
    {
        _products.Products.Add(CreateProduct("SKU-200", "shared-slug"));

        var result = await CreateHandler.Handle(
            new CreateProductCommand(
                "SKU-300",
                "shared-slug",
                "Another",
                null,
                "USD",
                10m,
                null,
                null,
                null,
                false,
                "Active",
                "en"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.SlugAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProduct_With_Invalid_Sku_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateProductCommand(
                "ab",
                "valid-slug",
                "Name",
                null,
                "USD",
                10m,
                null,
                null,
                null,
                false,
                "Draft",
                "en"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateProduct_Updates_Details_And_Raises_Event()
    {
        var product = CreateProduct("SKU-400", "original-slug", name: "Original");
        _products.Products.Add(product);

        var result = await UpdateHandler.Handle(
            new UpdateProductCommand(
                product.Id,
                "new-slug",
                "Updated Name",
                "New description",
                null,
                null,
                null,
                null,
                null,
                true,
                "Active",
                "en"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-slug", product.Slug);
        Assert.Equal("Updated Name", Assert.Single(product.Translations).Name);
        Assert.True(product.IsFeatured);
        Assert.Equal(1, _unitOfWork.SaveCount);

        Assert.IsType<ProductUpdated>(Assert.Single(product.DomainEvents.OfType<ProductUpdated>()));
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task UpdateProduct_With_Unknown_Id_Returns_NotFound()
    {
        var result = await UpdateHandler.Handle(
            new UpdateProductCommand(Guid.NewGuid(), "slug", "Name", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.ProductNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateProduct_With_Existing_Slug_On_Other_Product_Returns_Conflict()
    {
        var product = CreateProduct("SKU-500", "mine");
        _products.Products.Add(product);
        _products.Products.Add(CreateProduct("SKU-600", "taken"));

        var result = await UpdateHandler.Handle(
            new UpdateProductCommand(
                product.Id,
                "taken",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.SlugAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task UpdateProduct_With_No_Fields_Returns_Validation_Failure()
    {
        var product = CreateProduct("SKU-700", "slug");
        _products.Products.Add(product);

        var result = await UpdateHandler.Handle(
            new UpdateProductCommand(product.Id, null, null, null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeactivateProduct_Sets_Inactive_And_Raises_Event()
    {
        var product = CreateProduct("SKU-800", "active-slug", status: ProductStatus.Active);
        _products.Products.Add(product);

        var result = await DeactivateHandler.Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductStatus.Inactive, product.Status);
        Assert.True(product.IsDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);

        Assert.IsType<ProductDeactivated>(Assert.Single(product.DomainEvents.OfType<ProductDeactivated>()));
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task DeactivateProduct_With_Unknown_Id_Returns_NotFound()
    {
        var result = await DeactivateHandler.Handle(
            new DeactivateProductCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.ProductNotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProduct_With_Unsupported_Currency_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateProductCommand(
                "SKU-JPY",
                "jpy-product",
                "Name",
                null,
                "JPY",
                10m,
                null,
                null,
                null,
                false,
                "Draft",
                "en"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_products.Products);
    }

    [Fact]
    public async Task CreateProduct_With_Unsupported_Locale_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateProductCommand(
                "SKU-XX",
                "xx-product",
                "Name",
                null,
                "USD",
                10m,
                null,
                null,
                null,
                false,
                "Draft",
                "xx"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_products.Products);
    }

    [Fact]
    public async Task UpdateProduct_With_Unsupported_Currency_Returns_Validation_Failure()
    {
        var product = CreateProduct("SKU-900", "slug");
        _products.Products.Add(product);

        var result = await UpdateHandler.Handle(
            new UpdateProductCommand(product.Id, null, null, null, "JPY", 10m, null, null, null, null, null, "en"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
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

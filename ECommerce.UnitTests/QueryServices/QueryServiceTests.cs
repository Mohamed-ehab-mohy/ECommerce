using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Queries;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests.QueryServices;

public static class DbContextHelper
{
    public static StoreDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StoreDbContext(options);
    }

    public static void ConfigureMapping()
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(ECommerce.UseCases.Products.MappingConfig).Assembly);
    }
}

public class ProductQueryServiceTests : IDisposable
{
    private readonly StoreDbContext _dbContext;
    private readonly ProductQueryService _service;

    public ProductQueryServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        DbContextHelper.ConfigureMapping();
        _service = new ProductQueryService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllNonDeletedProducts()
    {
        var brand = ProductBrand.Create("Nike");
        var type = ProductType.Create("Shoes");
        _dbContext.ProductBrands.Add(brand.Data!);
        _dbContext.ProductTypes.Add(type.Data!);
        await _dbContext.SaveChangesAsync();

        var product1 = Product.Create("Air Max", "Running shoes", "img1.jpg", 120m, brand.Data!.Id, type.Data!.Id);
        var product2 = Product.Create("Air Jordan", "Basketball shoes", "img2.jpg", 150m, brand.Data!.Id, type.Data!.Id);
        _dbContext.Products.Add(product1.Data!);
        _dbContext.Products.Add(product2.Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllProductsAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldExcludeDeletedProducts()
    {
        var brand = ProductBrand.Create("Nike");
        var type = ProductType.Create("Shoes");
        _dbContext.ProductBrands.Add(brand.Data!);
        _dbContext.ProductTypes.Add(type.Data!);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Air Max", "Running shoes", "img1.jpg", 120m, brand.Data!.Id, type.Data!.Id);
        _dbContext.Products.Add(product.Data!);
        await _dbContext.SaveChangesAsync();

        product.Data!.MarkAsDeleted();
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllProductsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnCorrectDtoMapping()
    {
        var brand = ProductBrand.Create("Adidas");
        var type = ProductType.Create("Sneakers");
        _dbContext.ProductBrands.Add(brand.Data!);
        _dbContext.ProductTypes.Add(type.Data!);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Ultraboost", "Running shoe", "img.jpg", 180m, brand.Data!.Id, type.Data!.Id);
        _dbContext.Products.Add(product.Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllProductsAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ultraboost");
        result[0].Price.Should().Be(180m);
        result[0].ProductBrand.Should().Be("Adidas");
        result[0].ProductType.Should().Be("Sneakers");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenExists()
    {
        var brand = ProductBrand.Create("Nike");
        var type = ProductType.Create("Shoes");
        _dbContext.ProductBrands.Add(brand.Data!);
        _dbContext.ProductTypes.Add(type.Data!);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Air Max", "Running shoes", "img1.jpg", 120m, brand.Data!.Id, type.Data!.Id);
        _dbContext.Products.Add(product.Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetByIdAsync(product.Data!.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Air Max");
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenDeleted()
    {
        var brand = ProductBrand.Create("Nike");
        var type = ProductType.Create("Shoes");
        _dbContext.ProductBrands.Add(brand.Data!);
        _dbContext.ProductTypes.Add(type.Data!);
        await _dbContext.SaveChangesAsync();

        var product = Product.Create("Air Max", "Running shoes", "img1.jpg", 120m, brand.Data!.Id, type.Data!.Id);
        _dbContext.Products.Add(product.Data!);
        await _dbContext.SaveChangesAsync();

        product.Data!.MarkAsDeleted();
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetByIdAsync(product.Data!.Id);

        result.Should().BeNull();
    }
}

public class BrandQueryServiceTests : IDisposable
{
    private readonly StoreDbContext _dbContext;
    private readonly BrandQueryService _service;

    public BrandQueryServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        DbContextHelper.ConfigureMapping();
        _service = new BrandQueryService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllNonDeletedBrands()
    {
        _dbContext.ProductBrands.Add(ProductBrand.Create("Nike").Data!);
        _dbContext.ProductBrands.Add(ProductBrand.Create("Adidas").Data!);
        _dbContext.ProductBrands.Add(ProductBrand.Create("Puma").Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedBrands()
    {
        var brand = ProductBrand.Create("Nike");
        _dbContext.ProductBrands.Add(brand.Data!);
        await _dbContext.SaveChangesAsync();

        brand.Data!.MarkAsDeleted();
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCorrectDtoMapping()
    {
        _dbContext.ProductBrands.Add(ProductBrand.Create("Adidas").Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Adidas");
    }
}

public class TypeQueryServiceTests : IDisposable
{
    private readonly StoreDbContext _dbContext;
    private readonly TypeQueryService _service;

    public TypeQueryServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        DbContextHelper.ConfigureMapping();
        _service = new TypeQueryService(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllNonDeletedTypes()
    {
        _dbContext.ProductTypes.Add(ProductType.Create("Electronics").Data!);
        _dbContext.ProductTypes.Add(ProductType.Create("Clothing").Data!);
        _dbContext.ProductTypes.Add(ProductType.Create("Shoes").Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedTypes()
    {
        var type = ProductType.Create("Electronics");
        _dbContext.ProductTypes.Add(type.Data!);
        await _dbContext.SaveChangesAsync();

        type.Data!.MarkAsDeleted();
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCorrectDtoMapping()
    {
        _dbContext.ProductTypes.Add(ProductType.Create("Sports").Data!);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Sports");
    }
}

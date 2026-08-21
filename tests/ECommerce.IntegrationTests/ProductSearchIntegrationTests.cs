using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Search;
using ECommerce.UseCases.Catalog.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
public sealed class ProductSearchIntegrationTests
{
    private readonly IntegrationFixture _fixture;

    public ProductSearchIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Search_By_Name_Returns_Matching_Products()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var headphones = CreateProduct("SKU-S-001", "wireless-headphones", "Wireless Noise Cancelling Headphones");
        var keyboard = CreateProduct("SKU-S-002", "mechanical-keyboard", "Mechanical Keyboard");
        context.Products.AddRange(headphones, keyboard);
        await context.SaveChangesAsync();

        await new ProductSearchIndexSynchronizer(context).UpsertAsync(headphones.Id, CancellationToken.None);
        await new ProductSearchIndexSynchronizer(context).UpsertAsync(keyboard.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria("wireless", "en", null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(headphones.Id, Assert.Single(page.Items).Id);
    }

    [SkippableFact]
    public async Task Search_Is_Tolerant_To_Typographical_Errors()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var product = CreateProduct("SKU-S-003", "headphones", "Wireless Headphones");
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await new ProductSearchIndexSynchronizer(context).UpsertAsync(product.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria("heaphones", "en", null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Contains(page.Items, item => item.Id == product.Id);
    }

    [SkippableFact]
    public async Task Search_Filters_By_Brand_And_Category()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var now = DateTime.UtcNow;
        var brand = Brand.Create("Sony", null, null, now);
        var category = Category.Create("Audio", "audio", null, 1, 0, now);
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var matching = CreateProduct("SKU-S-004", "sony-headphones", "Sony WH Headphones", brand.Id, category.Id);
        var other = CreateProduct("SKU-S-005", "other-headphones", "Other Headphones");
        context.Products.AddRange(matching, other);
        await context.SaveChangesAsync();

        var synchronizer = new ProductSearchIndexSynchronizer(context);
        await synchronizer.UpsertAsync(matching.Id, CancellationToken.None);
        await synchronizer.UpsertAsync(other.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria(null, "en", category.Id, brand.Id, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(matching.Id, Assert.Single(page.Items).Id);
    }

    [SkippableFact]
    public async Task Search_Filters_By_Price_Range()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var cheap = CreateProduct("SKU-S-006", "cheap", "Cheap Headphones", listAmount: 100m);
        var expensive = CreateProduct("SKU-S-007", "expensive", "Premium Headphones", listAmount: 300m);
        context.Products.AddRange(cheap, expensive);
        await context.SaveChangesAsync();

        var synchronizer = new ProductSearchIndexSynchronizer(context);
        await synchronizer.UpsertAsync(cheap.Id, CancellationToken.None);
        await synchronizer.UpsertAsync(expensive.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria(null, "en", null, null, 50m, 200m, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(cheap.Id, Assert.Single(page.Items).Id);
    }

    [SkippableFact]
    public async Task Search_Returns_Facets_With_Consistent_Counts()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var now = DateTime.UtcNow;
        var brand = Brand.Create("Sony", null, null, now);
        context.Brands.Add(brand);
        await context.SaveChangesAsync();

        var sonyOne = CreateProduct("SKU-S-008", "sony-one", "Sony Headphones One", brand.Id, listAmount: 40m);
        var sonyTwo = CreateProduct("SKU-S-009", "sony-two", "Sony Headphones Two", brand.Id, listAmount: 120m);
        var other = CreateProduct("SKU-S-010", "other", "Other Headphones", listAmount: 600m);
        context.Products.AddRange(sonyOne, sonyTwo, other);
        await context.SaveChangesAsync();

        var synchronizer = new ProductSearchIndexSynchronizer(context);
        await synchronizer.UpsertAsync(sonyOne.Id, CancellationToken.None);
        await synchronizer.UpsertAsync(sonyTwo.Id, CancellationToken.None);
        await synchronizer.UpsertAsync(other.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria(null, "en", null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(3, page.TotalCount);
        var sonyFacet = Assert.Single(page.Facets.Brands);
        Assert.Equal(brand.Id, sonyFacet.Id);
        Assert.Equal(2, sonyFacet.Count);

        Assert.Equal(1, page.Facets.PriceRanges.Single(range => range.Key == "under-50").Count);
        Assert.Equal(1, page.Facets.PriceRanges.Single(range => range.Key == "100-250").Count);
        Assert.Equal(1, page.Facets.PriceRanges.Single(range => range.Key == "over-500").Count);
    }

    [SkippableFact]
    public async Task Search_Excludes_Deactivated_Products()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var active = CreateProduct("SKU-S-011", "active", "Active Headphones");
        var deactivated = CreateProduct("SKU-S-012", "inactive", "Inactive Headphones");
        deactivated.Deactivate();
        context.Products.AddRange(active, deactivated);
        await context.SaveChangesAsync();

        var synchronizer = new ProductSearchIndexSynchronizer(context);
        await synchronizer.UpsertAsync(active.Id, CancellationToken.None);
        await synchronizer.UpsertAsync(deactivated.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria(null, "en", null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(active.Id, Assert.Single(page.Items).Id);
    }

    [SkippableFact]
    public async Task Upsert_Refreshes_Search_Document_On_Product_Update()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await ResetAsync(context);

        var product = CreateProduct("SKU-S-013", "headphones", "Old Name");
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var synchronizer = new ProductSearchIndexSynchronizer(context);
        await synchronizer.UpsertAsync(product.Id, CancellationToken.None);

        product.UpdateDetails(null, null, null, null, null, "en", "New Premium Name", null, null, null, null, DateTime.UtcNow);
        await context.SaveChangesAsync();
        await synchronizer.UpsertAsync(product.Id, CancellationToken.None);

        var page = await new ProductSearchRepository(context).SearchAsync(
            new ProductSearchCriteria("premium", "en", null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(product.Id, Assert.Single(page.Items).Id);
    }

    private static Product CreateProduct(
        string sku,
        string slug,
        string name,
        Guid? brandId = null,
        Guid? categoryId = null,
        decimal listAmount = 100m) =>
        Product.Create(
            sku,
            slug,
            "en",
            name,
            null,
            "USD",
            listAmount,
            null,
            categoryId,
            brandId,
            false,
            ProductStatus.Active,
            DateTime.UtcNow);

    private static async Task ResetAsync(ECommerceDbContext context) =>
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE product_search_documents, products, brands, categories CASCADE;");

    private ECommerceDbContext CreateContext()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fixture.PostgresConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        return new(
            new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseNpgsql(dataSourceBuilder.Build())
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
    }
}

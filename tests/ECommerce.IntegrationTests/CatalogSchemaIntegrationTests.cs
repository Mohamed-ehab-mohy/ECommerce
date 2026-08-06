using ECommerce.Domain.Catalog;
using ECommerce.Domain.Inventory;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ECommerce.IntegrationTests;

public sealed class CatalogSchemaIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public CatalogSchemaIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CatalogMigration_Is_Additive_And_Applies()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var tables = await QueryTablesAsync();
        Assert.Contains(tables, table => table == "products");
        Assert.Contains(tables, table => table == "product_variants");
        Assert.Contains(tables, table => table == "categories");
        Assert.Contains(tables, table => table == "category_hierarchy");
        Assert.Contains(tables, table => table == "brands");
        Assert.Contains(tables, table => table == "product_translations");
        Assert.Contains(tables, table => table == "product_prices");
        Assert.Contains(tables, table => table == "warehouses");
        Assert.Contains(tables, table => table == "roles");
        Assert.Contains(tables, table => table == "role_permissions");
        Assert.Contains(tables, table => table == "user_roles");
        Assert.Contains(tables, table => table == "carts");
        Assert.Contains(tables, table => table == "cart_items");
    }

    [SkippableFact]
    public async Task Product_Sku_Uniqueness_Is_Enforced()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        context.Products.Add(Product.Create("SKU-001", "slug-001", "en", "Test Product", null, "USD", 10m, null, null, null, false, ProductStatus.Active, now));

        await context.SaveChangesAsync();

        context.Products.Add(Product.Create("SKU-001", "slug-002", "en", "Test Product", null, "USD", 10m, null, null, null, false, ProductStatus.Active, now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Product_Slug_Uniqueness_Is_Enforced()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        context.Products.Add(Product.Create("SKU-002", "same-slug", "en", "Test Product", null, "USD", 10m, null, null, null, false, ProductStatus.Active, now));

        await context.SaveChangesAsync();

        context.Products.Add(Product.Create("SKU-003", "same-slug", "en", "Test Product", null, "USD", 10m, null, null, null, false, ProductStatus.Active, now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Variant_Sku_Uniqueness_Is_Enforced()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var product = Product.Create("SKU-004", "slug-004", "en", "Test Product", null, "USD", 10m, null, null, null, false, ProductStatus.Active, now);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.ProductVariants.Add(ProductVariant.Create(product.Id, "VAR-001", "Variant One", now));
        await context.SaveChangesAsync();

        context.ProductVariants.Add(ProductVariant.Create(product.Id, "VAR-001", "Variant Duplicate", now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Price_Constraints_Are_Enforced()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        var product = Product.Create("SKU-005", "slug-005", "en", "Test Product", null, "USD", 10m, null, null, null, false, ProductStatus.Active, now);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.ProductPrices.Add(ProductPrice.Create(product.Id, "EUR", 0m, null, now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Warehouse_Code_Uniqueness_Is_Enforced()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var now = DateTime.UtcNow;
        context.Warehouses.Add(Warehouse.Create("CAI-01", "Cairo Hub", "Downtown, Cairo", "Africa/Cairo", WarehouseStatus.Active, now));

        await context.SaveChangesAsync();

        context.Warehouses.Add(Warehouse.Create("CAI-01", "Duplicate", "Somewhere", "UTC", WarehouseStatus.Active, now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private ECommerceDbContext CreateContext()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fixture.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        return new(
            new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseNpgsql(dataSourceBuilder.Build())
                .Options);
    }

    private async Task<IReadOnlyCollection<string>> QueryTablesAsync()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name IN (
                'products', 'product_variants', 'categories',
                'category_hierarchy', 'brands', 'product_translations', 'product_prices',
                'warehouses',
                'roles', 'role_permissions', 'user_roles',
                'carts', 'cart_items')
            """,
            connection);
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}

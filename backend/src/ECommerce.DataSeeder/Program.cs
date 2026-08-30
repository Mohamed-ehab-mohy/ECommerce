using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Tenants;
using ECommerce.Domain.Identity;
using ECommerce.Domain.Wallets;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.DataSeeder;

internal static class SeedRoleIds
{
    public static readonly Guid Customer = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Staff = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Finance = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Admin = new("44444444-4444-4444-4444-444444444444");
    public static readonly Guid SuperAdmin = new("55555555-5555-5555-5555-555555555555");
}

internal class Program
{
    internal static async Task Main(string[] args)
    {
        Console.WriteLine("Starting E-Commerce Data Seeder...");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=127.0.0.1;Port=5433;Database=ecommerce;Username=ecommerce;Password=ecommerce_dev_pw";

        Console.WriteLine($"Using Database Connection: {connectionString}");

        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        var services = new ServiceCollection();
        services.AddDbContext<ECommerceDbContext>(options =>
        {
            options.UseNpgsql(dataSource);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        Console.WriteLine("Applying Database Migrations...");
        await context.Database.MigrateAsync();

        Console.WriteLine("Wiping existing data (Truncating tables)...");
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                TRUNCATE TABLE ""Tenants"", roles, customers, ""Wallets"", carts, orders, categories, products, warehouses CASCADE;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note on truncation: {ex.Message}");
        }

        Console.WriteLine("Generating Mock Data using Bogus...");

        var utcNow = DateTime.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12);

        // 1. Tenants
        var tenantFaker = new Faker<Tenant>()
            .CustomInstantiator(f => new Tenant(f.Company.CompanyName(), f.Internet.DomainName()));

        var tenants = tenantFaker.Generate(3);
        await context.Tenants.AddRangeAsync(tenants);
        await context.SaveChangesAsync();
        Console.WriteLine($"Created {tenants.Count} Tenants.");

        foreach (var tenant in tenants)
        {
            // Set TenantScope so EF Core Global Query Filter works
            TenantScope.Current = tenant.Id;

            // Seed Roles using Role.Create (generates dynamic IDs)
            // Then assign permissions using the Role's AssignPermissions method
            var adminRole = Role.Create($"Admin_{tenant.Id}", "Administrator role with full access", utcNow);
            adminRole.AssignPermissions(new[]
            {
                "catalog.product.write",
                "catalog.product.delete",
                "roles.read",
                "roles.write",
                "roles.permissions.write",
                "customers.read",
                "audit.read"
            }, utcNow);

            var customerRole = Role.Create($"Customer_{tenant.Id}", "Default customer role", utcNow);
            customerRole.AssignPermissions(new[] { "customers.read" }, utcNow);

            context.Roles.Add(adminRole);
            context.Roles.Add(customerRole);
            await context.SaveChangesAsync();

            // Seed Customers (Users)
            var customerFaker = new Faker<Customer>()
                .CustomInstantiator(f => Customer.Register(
                    f.Internet.Email(),
                    f.Name.FullName(),
                    "en-US",
                    "USD",
                    passwordHash,
                    "fake-token-hash",
                    utcNow.AddDays(1),
                    "fake-token"
                ));

            var customersList = customerFaker.Generate(10);
            foreach (var c in customersList)
            {
                c.VerifyEmail("fake-token-hash", utcNow);
                context.Set<Customer>().Add(c);
            }
            await context.SaveChangesAsync();

            // Link Users to Customer Role
            foreach (var c in customersList)
            {
                context.UserRoles.Add(UserRole.Create(c.Id, customerRole.Id));

                // Seed Wallet
                var wallet = Wallet.Create(c.Id, "USD");
                wallet.Credit(new Faker().Random.Number(100, 5000), Guid.NewGuid().ToString());
                wallet.AddPoints(new Faker().Random.Number(50, 500), Guid.NewGuid().ToString());
                context.Set<Wallet>().Add(wallet);

                // Seed empty cart
                var cart = Cart.Create(c.Id.ToString(), "USD", utcNow.AddDays(7), utcNow);
                context.Carts.Add(cart);
            }
            await context.SaveChangesAsync();

            // 2. Warehouses
            var warehouseFaker = new Faker<Warehouse>()
                .CustomInstantiator(f => Warehouse.Create(
                    f.Random.AlphaNumeric(8).ToUpper(),
                    f.Company.CompanyName() + " Warehouse",
                    f.Address.FullAddress(),
                    "UTC",
                    WarehouseStatus.Active,
                    utcNow));

            var warehouses = warehouseFaker.Generate(2);
            await context.Warehouses.AddRangeAsync(warehouses);
            await context.SaveChangesAsync();

            // 3. Categories
            var categoryFaker = new Faker<Category>()
                .CustomInstantiator(f => Category.Create(
                    f.Commerce.Categories(1)[0],
                    f.Commerce.Categories(1)[0].ToLower().Replace(" ", "-") + f.Random.Number(1, 999),
                    null,
                    f.Random.Number(1, 10),
                    1,
                    utcNow));

            var categories = categoryFaker.Generate(5);
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // 4. Products & Variants
            var productFaker = new Faker<Product>()
                .CustomInstantiator(f => Product.Create(
                    f.Commerce.Ean13(), // sku
                    f.Commerce.ProductName().ToLower().Replace(" ", "-") + "-" + f.Random.Number(1000, 9999), // slug
                    "en", // locale
                    f.Commerce.ProductName(), // name
                    f.Commerce.ProductDescription(), // description
                    "USD", // currency
                    f.Random.Number(10, 500), // listAmount
                    null, // offerAmount
                    f.PickRandom(categories).Id, // categoryId
                    null, // brandId
                    f.Random.Bool(), // isFeatured
                    ProductStatus.Active, // status
                    utcNow, // utcNow
                    true // backorderable
                ));

            var products = productFaker.Generate(20);
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            var allVariants = new List<ProductVariant>();

            foreach (var product in products)
            {
                var variantFaker = new Faker<ProductVariant>()
                    .CustomInstantiator(f => ProductVariant.Create(
                        product.Id,
                        f.Commerce.Ean13() + f.Random.Number(1, 999),
                        f.Commerce.Ean8() + f.Random.Number(1, 999),
                        utcNow
                    ));

                var variants = variantFaker.Generate(new Faker().Random.Number(1, 4));
                await context.ProductVariants.AddRangeAsync(variants);
                await context.SaveChangesAsync();

                allVariants.AddRange(variants);

                // Generate stock for variants
                foreach (var variant in variants)
                {
                    foreach (var warehouse in warehouses)
                    {
                        var stockItem = StockItem.Create(variant.Sku, warehouse.Id, utcNow);
                        context.Entry(stockItem).Property("OnHand").CurrentValue = new Faker().Random.Number(10, 1000);
                        await context.StockItems.AddAsync(stockItem);
                    }
                }
                await context.SaveChangesAsync();
            }

            // Generate some mock orders for the first 5 customers
            var orderCustomers = customersList.Take(5).ToList();
            foreach (var oc in orderCustomers)
            {
                var pickedVariant = allVariants[new Faker().Random.Number(0, allVariants.Count - 1)];
                var lineItem = new PriceSnapshotItem(
                    pickedVariant.ProductId,
                    pickedVariant.Sku,
                    "Product Name",
                    100m,
                    100m,
                    2,
                    null);


                var totals = new TotalsSnapshot(200m, 0m, 0m, 10m, 21m, 231m, 0.1m);
                var snapshot = new PriceSnapshot(new List<PriceSnapshotItem> { lineItem }, totals);
                var addr = new AddressSnapshot("Jane Doe", "123 Main St", string.Empty, "City", "State", "Country", "12345");

                var order = Order.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    oc.Id,
                    oc.Email,
                    "USD",
                    $"ORD-{new Faker().Random.Number(1000, 9999)}",
                    snapshot,
                    addr,
                    addr,
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid(),
                    utcNow);

                context.Orders.Add(order);
            }
            await context.SaveChangesAsync();

            Console.WriteLine($"Seeded Tenant: {tenant.Name} with complete data (Users, Wallets, Catalog, Inventory, Orders).");
        }

        Console.WriteLine("Database Seeding Completed Successfully! You can now log in using any generated user with password: Password123!");
    }
}

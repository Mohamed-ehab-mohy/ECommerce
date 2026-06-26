using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductBrandSeeder : IDataSeeder
{
    private readonly StoreDbContext _context;

    public ProductBrandSeeder(StoreDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await JsonSeeder.SeedIfEmptyAsync<ProductBrand, ProductBrandSeedModel>(
            _context.Set<ProductBrand>(),
            "brands.json",
            model => ProductBrand.Create(model.Name, model.Id),
            ct);
    }
}
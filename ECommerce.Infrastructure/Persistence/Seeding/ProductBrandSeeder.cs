using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductBrandSeeder : IDataSeeder
{
    private readonly StoreDbContext _dbContext;

    public ProductBrandSeeder(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int Order => 1;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await JsonSeeder.SeedIfEmptyAsync<ProductBrand, ProductBrandSeedModel>(
            _dbContext.ProductBrands,
            "brands.json",
            model => ProductBrand.Create(model.Name, model.Id).Data!,
            ct);
    }
}

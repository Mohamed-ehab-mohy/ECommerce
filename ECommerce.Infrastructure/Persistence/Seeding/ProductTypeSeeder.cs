using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductTypeSeeder : IDataSeeder
{
    private readonly StoreDbContext _dbContext;

    public ProductTypeSeeder(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int Order => 2;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await JsonSeeder.SeedIfEmptyAsync<ProductType, ProductTypeSeedModel>(
            _dbContext.ProductTypes,
            "types.json",
            model => ProductType.Create(model.Name, model.Id),
            ct);
    }
}
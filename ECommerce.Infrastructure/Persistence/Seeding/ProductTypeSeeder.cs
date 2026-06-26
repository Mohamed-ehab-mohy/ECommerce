using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductTypeSeeder : IDataSeeder
{
    private readonly StoreDbContext _context;

    public ProductTypeSeeder(StoreDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await JsonSeeder.SeedIfEmptyAsync<ProductType, ProductTypeSeedModel>(
            _context.Set<ProductType>(),
            "types.json",
            model => ProductType.Create(model.Name, model.Id),
            ct);
    }
}
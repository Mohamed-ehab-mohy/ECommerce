using ECommerce.Infrastructure.Data.DbContexts;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class DatabaseSeeder
{
    private readonly StoreDbContext _dbContext;
    private readonly IEnumerable<IDataSeeder> _seeders;

    public DatabaseSeeder(StoreDbContext dbContext, IEnumerable<IDataSeeder> seeders)
    {
        _dbContext = dbContext;
        _seeders = seeders;
    }

    public async Task SeedAllAsync(CancellationToken ct = default)
    {
        foreach (var seeder in _seeders.OrderBy(s => s.Order))
        {
            await seeder.SeedAsync(ct);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
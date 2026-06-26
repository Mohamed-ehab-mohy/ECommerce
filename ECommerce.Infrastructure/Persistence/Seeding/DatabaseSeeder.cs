namespace ECommerce.Infrastructure.Persistence.Seeding;

public class DatabaseSeeder
{
    private readonly IEnumerable<IDataSeeder> _seeders;

    public DatabaseSeeder(IEnumerable<IDataSeeder> seeders)
    {
        _seeders = seeders;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var seeder in _seeders.OrderBy(s => s.Order))
        {
            await seeder.SeedAsync(ct);
        }
    }
}
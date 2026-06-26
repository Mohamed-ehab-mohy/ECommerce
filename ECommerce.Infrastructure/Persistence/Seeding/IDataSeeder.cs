namespace ECommerce.Infrastructure.Persistence.Seeding;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
namespace ECommerce.Infrastructure.Persistence.Seeding;

public interface IDataSeeder
{
    int Order { get; }
    Task SeedAsync(CancellationToken ct = default);
}
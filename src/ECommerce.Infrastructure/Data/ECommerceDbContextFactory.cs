using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Infrastructure.Data;

public sealed class ECommerceDbContextFactory : IDesignTimeDbContextFactory<ECommerceDbContext>
{
    public ECommerceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=ecommerce;Username=ecommerce;Password=ecommerce_dev_pw";
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ECommerceDbContext(options);
    }
}

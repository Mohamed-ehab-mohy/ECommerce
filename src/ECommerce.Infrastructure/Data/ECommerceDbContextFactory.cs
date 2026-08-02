using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace ECommerce.Infrastructure.Data;

public sealed class ECommerceDbContextFactory : IDesignTimeDbContextFactory<ECommerceDbContext>
{
    public ECommerceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=ecommerce;Username=ecommerce;Password=ecommerce_dev_pw";
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;
        return new ECommerceDbContext(options);
    }
}

using ECommerce.Domain.Repositories;
using ECommerce.Infrastructure.Data.DbContexts;
using ECommerce.Infrastructure.Persistence.Interceptors;
using ECommerce.Infrastructure.Persistence.Seeding;
using ECommerce.Infrastructure.Queries;
using ECommerce.Infrastructure.Repositories;
using ECommerce.UseCases.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StoreDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IDataSeeder, ProductBrandSeeder>();
        services.AddScoped<IDataSeeder, ProductTypeSeeder>();
        services.AddScoped<DatabaseSeeder>();

        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductQueryService, ProductQueryService>();

        return services;
    }
}

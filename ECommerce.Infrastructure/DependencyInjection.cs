using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Future: Add DbContext and Repositories
        // services.AddDbContext<AppDbContext>(options => ...);
        // services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}

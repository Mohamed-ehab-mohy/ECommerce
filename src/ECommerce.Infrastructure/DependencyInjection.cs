using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string postgresConnectionString)
    {
        services.AddDbContext<ECommerceDbContext>(options => options.UseNpgsql(postgresConnectionString));
        return services;
    }
}

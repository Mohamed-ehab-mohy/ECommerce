using ECommerce.Infrastructure;
using ECommerce.UseCases;

namespace ECommerce.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddApplication();
        services.AddInfrastructure();
        return services;
    }
}

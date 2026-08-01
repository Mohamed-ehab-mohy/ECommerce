using ECommerce.Infrastructure;
using ECommerce.UseCases;
using Microsoft.Extensions.Configuration;

namespace ECommerce.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration.GetConnectionString("Postgres")!);

        services.AddControllers();
        services.AddProblemDetails();

        return services;
    }
}

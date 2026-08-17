using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Resilience;

public static class DependencyInjection
{
    public static IServiceCollection AddResilience(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));

        services.AddResilienceEnricher();

        return services;
    }
}

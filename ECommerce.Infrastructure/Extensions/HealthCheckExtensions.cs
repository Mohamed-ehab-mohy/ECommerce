using ECommerce.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.Infrastructure.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthCheckInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var healthCheckConfiguration = configuration.GetSection("HealthChecks");

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"])
            .AddCheck<MemoryHealthCheck>(
                name: "memory",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "memory"]);

        return services;
    }
}

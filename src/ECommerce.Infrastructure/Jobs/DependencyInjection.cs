using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Jobs;

public static class DependencyInjection
{
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddHangfire(configuration => configuration
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true
                })
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseFilter(new AutomaticRetryAttribute { Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail }));

        services.AddHangfireServer(options => options.WorkerCount = 2);

        return services;
    }
}

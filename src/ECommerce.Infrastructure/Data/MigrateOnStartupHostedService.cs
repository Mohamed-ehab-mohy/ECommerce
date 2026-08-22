using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Data;

public sealed class MigrateOnStartupHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MigrateOnStartupHostedService> logger) : BackgroundService
{
    private const int DefaultMaxAttempts = 40;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxAttempts = configuration.GetValue("Database:MigrationStartupMaxAttempts", DefaultMaxAttempts);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await MigrateAsync(stoppingToken);
                logger.LogInformation("Database migrations applied on startup.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Database migration attempt {Attempt}/{MaxAttempts} failed.",
                    attempt,
                    maxAttempts);
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }

        throw new InvalidOperationException(
            $"Database migrations could not be applied after {maxAttempts} attempts.");
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

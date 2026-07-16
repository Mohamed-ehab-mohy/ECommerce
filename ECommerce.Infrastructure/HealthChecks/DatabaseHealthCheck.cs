using ECommerce.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly StoreDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(StoreDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: unable to connect.");

                return HealthCheckResult.Unhealthy(
                    "Database is not accessible.",
                    data: new Dictionary<string, object>
                    {
                        { "database", _dbContext.Database.GetConnectionString() ?? "unknown" }
                    });
            }

            return HealthCheckResult.Healthy(
                "Database is accessible and responding normally.",
                data: new Dictionary<string, object>
                {
                    { "database", _dbContext.Database.GetConnectionString() ?? "unknown" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check threw an exception.");

            return HealthCheckResult.Unhealthy(
                "An unexpected error occurred while checking database health.",
                exception: ex);
        }
    }
}

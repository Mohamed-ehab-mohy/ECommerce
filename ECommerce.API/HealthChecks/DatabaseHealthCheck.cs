using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public DatabaseHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Default");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is accessible and responding normally.");
        }
        catch (SqlException ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database is not accessible.",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "sqlError", ex.Number },
                    { "sqlState", ex.State }
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "An unexpected error occurred while checking database health.",
                exception: ex);
        }
    }
}

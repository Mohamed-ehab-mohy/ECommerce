using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ECommerce.IntegrationTests;

public sealed class PostgresIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public PostgresIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Migrations_Apply_To_Real_Postgres()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _fixture.ConnectionString);
        try
        {
            await using var context = new ECommerceDbContextFactory().CreateDbContext(Array.Empty<string>());
            await context.Database.MigrateAsync();
            var applied = await context.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, migration => migration.EndsWith("InitialMigration", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", previous);
        }
    }

    [SkippableFact]
    public async Task Connects_And_Runs_Query()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();
        Assert.Equal(1L, Convert.ToInt64(result));
    }
}

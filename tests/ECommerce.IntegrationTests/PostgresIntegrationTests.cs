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

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fixture.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        await using var context = new ECommerceDbContext(
            new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseNpgsql(dataSourceBuilder.Build())
                .Options);
        await context.Database.MigrateAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, migration => migration.EndsWith("InitialMigration", StringComparison.Ordinal));
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

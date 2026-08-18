using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ECommerce.IntegrationTests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private ECommerceDbContext? _dbContext;

    public string ConnectionString => _container!.GetConnectionString();

    public ECommerceDbContext DbContext => _dbContext!;

    public Task InitializeAsync()
    {
        if (!Docker.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        return _container.StartAsync();
    }

    public async Task EnsureDatabaseReadyAsync()
    {
        if (_dbContext is not null) return;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dbContext = new ECommerceDbContext(
            new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseNpgsql(dataSourceBuilder.Build())
                .Options);
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() =>
        _container is { } container && Docker.IsAvailable
            ? container.DisposeAsync().AsTask()
            : Task.CompletedTask;
}

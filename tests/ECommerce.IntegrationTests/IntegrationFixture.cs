using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;

namespace ECommerce.IntegrationTests;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private RabbitMqContainer? _rabbitMq;
    private ECommerceDbContext? _dbContext;

    public string PostgresConnectionString => _postgres!.GetConnectionString();
    public string RedisConnectionString => _redis!.GetConnectionString();
    public string RabbitMqConnectionString => _rabbitMq!.GetConnectionString();

    public ECommerceDbContext DbContext => _dbContext!;

    public Task InitializeAsync()
    {
        if (!Docker.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
        _redis = new RedisBuilder("redis:7-alpine").Build();
        _rabbitMq = new RabbitMqBuilder("rabbitmq:3.13-management").Build();

        return Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _rabbitMq.StartAsync());
    }

    public async Task EnsureDatabaseReadyAsync()
    {
        if (_dbContext is not null) return;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(PostgresConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dbContext = new ECommerceDbContext(
            new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseNpgsql(dataSourceBuilder.Build())
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => await _dbContext.Database.MigrateAsync());
    }

    public Task DisposeAsync() =>
        Docker.IsAvailable
            ? Task.WhenAll(
                _postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask,
                _redis?.DisposeAsync().AsTask() ?? Task.CompletedTask,
                _rabbitMq?.DisposeAsync().AsTask() ?? Task.CompletedTask)
            : Task.CompletedTask;
}

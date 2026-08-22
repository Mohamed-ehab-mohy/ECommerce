using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;

namespace ECommerce.IntegrationTests;

public sealed class IntegrationFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static PostgreSqlContainer? _postgres;
    private static RedisContainer? _redis;
    private static RabbitMqContainer? _rabbitMq;
    private static ECommerceDbContext? _dbContext;

    public static string GetPostgresConnectionString() =>
        _postgres is null
            ? throw new InvalidOperationException("Fixture not initialized")
            : _postgres.GetConnectionString() + ";Maximum Pool Size=15;Minimum Pool Size=0";

    public static string GetRedisConnectionString() => _redis?.GetConnectionString() ?? throw new InvalidOperationException("Fixture not initialized");
    public static string GetRabbitMqConnectionString() => _rabbitMq?.GetConnectionString().Replace("amqp://", "rabbitmq://") ?? throw new InvalidOperationException("Fixture not initialized");

    public string PostgresConnectionString => GetPostgresConnectionString();
    public string RedisConnectionString => GetRedisConnectionString();
    public string RabbitMqConnectionString => GetRabbitMqConnectionString();
    public ECommerceDbContext DbContext => _dbContext!;

    public async Task InitializeAsync()
    {
        if (_postgres is not null) return;
        if (!Docker.IsAvailable) return;

        await Lock.WaitAsync();
        try
        {
            if (_postgres is not null) return;

            _postgres = new PostgreSqlBuilder("postgres:16-alpine").WithCommand("-c", "max_connections=1000").Build();
            _redis = new RedisBuilder("redis:7-alpine").Build();
            _rabbitMq = new RabbitMqBuilder("rabbitmq:3.13-management").Build();

            await Task.WhenAll(
                _postgres.StartAsync(),
                _redis.StartAsync(),
                _rabbitMq.StartAsync());
        }
        finally
        {
            Lock.Release();
        }
    }

    public static async Task EnsureDatabaseReadyAsync()
    {
        if (_dbContext is not null) return;

        await Lock.WaitAsync();
        try
        {
            if (_dbContext is not null) return;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(GetPostgresConnectionString());
            dataSourceBuilder.EnableDynamicJson();
            _dbContext = new ECommerceDbContext(
                new DbContextOptionsBuilder<ECommerceDbContext>()
                    .UseNpgsql(dataSourceBuilder.Build())
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                    .Options);
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () => await _dbContext.Database.MigrateAsync());
        }
        finally
        {
            Lock.Release();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

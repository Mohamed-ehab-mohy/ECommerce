using Testcontainers.Redis;

namespace ECommerce.IntegrationTests;

public sealed class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() =>
        Docker.IsAvailable ? _container.StartAsync() : Task.CompletedTask;

    public Task DisposeAsync() =>
        Docker.IsAvailable ? _container.DisposeAsync().AsTask() : Task.CompletedTask;
}

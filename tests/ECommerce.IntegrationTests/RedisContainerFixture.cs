using Testcontainers.Redis;

namespace ECommerce.IntegrationTests;

public sealed class RedisContainerFixture : IAsyncLifetime
{
    private RedisContainer? _container;

    public string ConnectionString => _container!.GetConnectionString();

    public Task InitializeAsync()
    {
        if (!Docker.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _container = new RedisBuilder("redis:7-alpine").Build();
        return _container.StartAsync();
    }

    public Task DisposeAsync() =>
        _container is { } container && Docker.IsAvailable
            ? container.DisposeAsync().AsTask()
            : Task.CompletedTask;
}

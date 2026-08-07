using Testcontainers.RabbitMq;

namespace ECommerce.IntegrationTests;

public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

    public string ConnectionString => _container!.GetConnectionString();

    public Task InitializeAsync()
    {
        if (!Docker.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _container = new RabbitMqBuilder("rabbitmq:3.13-management").Build();
        return _container.StartAsync();
    }

    public Task DisposeAsync() =>
        _container is { } container && Docker.IsAvailable
            ? container.DisposeAsync().AsTask()
            : Task.CompletedTask;
}

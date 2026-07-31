using Testcontainers.RabbitMq;

namespace ECommerce.IntegrationTests;

public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:3.13-management").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() =>
        Docker.IsAvailable ? _container.StartAsync() : Task.CompletedTask;

    public Task DisposeAsync() =>
        Docker.IsAvailable ? _container.DisposeAsync().AsTask() : Task.CompletedTask;
}

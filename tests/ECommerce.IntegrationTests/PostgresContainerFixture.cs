using Testcontainers.PostgreSql;

namespace ECommerce.IntegrationTests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() =>
        Docker.IsAvailable ? _container.StartAsync() : Task.CompletedTask;

    public Task DisposeAsync() =>
        Docker.IsAvailable ? _container.DisposeAsync().AsTask() : Task.CompletedTask;
}

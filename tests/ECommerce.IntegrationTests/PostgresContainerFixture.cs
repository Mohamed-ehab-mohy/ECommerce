using Testcontainers.PostgreSql;

namespace ECommerce.IntegrationTests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString => _container!.GetConnectionString();

    public Task InitializeAsync()
    {
        if (!Docker.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        return _container.StartAsync();
    }

    public Task DisposeAsync() =>
        _container is { } container && Docker.IsAvailable
            ? container.DisposeAsync().AsTask()
            : Task.CompletedTask;
}

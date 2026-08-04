using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ECommerce.IntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("Fixture not initialized");

    public CapturingEmailSender EmailSender { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!Docker.IsAvailable)
        {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await _container.StartAsync();

        var emailSender = new CapturingEmailSender();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] = _container!.GetConnectionString()
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPasswordBreachChecker>();
                    services.AddSingleton<IPasswordBreachChecker>(new NonBreachedPasswordChecker());

                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(emailSender);

                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(_container!.GetConnectionString());
                    dataSourceBuilder.EnableDynamicJson();
                    var dataSource = dataSourceBuilder.Build();
                    services.RemoveAll<NpgsqlDataSource>();
                    services.RemoveAll<DbContextOptions<ECommerceDbContext>>();
                    services.RemoveAll<ECommerceDbContext>();
                    services.AddDbContext<ECommerceDbContext>(options => options
                        .UseNpgsql(dataSource)
                        .AddInterceptors(new DomainEventsInterceptor()));
                });
            });

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        EmailSender = emailSender;
        Client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();

        return _container is { } container && Docker.IsAvailable
            ? container.DisposeAsync().AsTask()
            : Task.CompletedTask;
    }
}

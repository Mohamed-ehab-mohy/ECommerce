using System.Linq;
using System.Text.Json;
using ECommerce.Domain.Abstractions;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Messaging;
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
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace ECommerce.IntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private RedisContainer? _redisContainer;
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
        _redisContainer = new RedisBuilder("redis:7-alpine").Build();
        await Task.WhenAll(_container.StartAsync(), _redisContainer.StartAsync());

        var emailSender = new CapturingEmailSender();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] = _container!.GetConnectionString(),
                        ["ConnectionStrings:Redis"] = _redisContainer!.GetConnectionString(),
                        ["ConnectionStrings:RabbitMq"] = "",
                        ["Hangfire:Disabled"] = "true"
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPasswordBreachChecker>();
                    services.AddSingleton<IPasswordBreachChecker>(new NonBreachedPasswordChecker());

                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(emailSender);

                    services.RemoveAll<IConnectionMultiplexer>();
                    services.AddSingleton<IConnectionMultiplexer>(_ =>
                        ConnectionMultiplexer.Connect(_redisContainer!.GetConnectionString()));

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
            await WaitForMigrationsAsync(dbContext);
        }

        EmailSender = emailSender;
        Client = _factory.CreateClient();
    }

    public async Task ProcessOutboxAsync()
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
        var metrics = scope.ServiceProvider.GetRequiredService<OutboxMetrics>();
        var postCommit = scope.ServiceProvider.GetRequiredService<PostCommitActions>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var messages = await dbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT * FROM "outbox_events"
                    WHERE "processed_on" IS NULL
                    ORDER BY "occurred_on"
                    LIMIT 20
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync();

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync();
                return;
            }

            foreach (var message in messages)
            {
                try
                {
                    var domainEvent = Deserialize(message.EventType, message.Content);
                    if (domainEvent is null)
                    {
                        message.Attempts++;
                        message.Error = $"Unknown event type: {message.EventType}";
                        message.ProcessedOn = message.Attempts >= 5 ? DateTime.UtcNow : null;
                        continue;
                    }

                    await publisher.PublishAsync(message, domainEvent, CancellationToken.None);
                    message.ProcessedOn = DateTime.UtcNow;
                    message.Error = null;
                }
                catch (Exception exception)
                {
                    message.Attempts++;
                    message.Error = exception.Message;
                    message.ProcessedOn = message.Attempts >= 5 ? DateTime.UtcNow : null;
                }
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            await postCommit.ExecuteAsync();
        });
    }

    private static async Task WaitForMigrationsAsync(ECommerceDbContext dbContext)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            try
            {
                var pending = await dbContext.Database.GetPendingMigrationsAsync();
                if (!pending.Any())
                {
                    return;
                }
            }
            catch (NpgsqlException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException("Database migrations did not complete in time.");
    }

    private static IDomainEvent? Deserialize(string eventType, string content)
    {
        var type = typeof(IDomainEvent).Assembly.GetType(eventType);

        return type is null
            ? null
            : JsonSerializer.Deserialize(content, type) as IDomainEvent;
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();

        if (Docker.IsAvailable)
        {
            if (_container is { } container)
                await container.DisposeAsync().AsTask();
            if (_redisContainer is { } redis)
                await redis.DisposeAsync().AsTask();
        }
    }
}

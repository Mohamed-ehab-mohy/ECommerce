using System.Data;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("Fixture not initialized");

    public CapturingEmailSender EmailSender { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("Telemetry__Disabled", "true");

        if (!Docker.IsAvailable)
        {
            return;
        }

        await new IntegrationFixture().InitializeAsync();

        var emailSender = new CapturingEmailSender();
        var postgresConnectionString = IntegrationFixture.GetPostgresConnectionString();
        var redisConnectionString = IntegrationFixture.GetRedisConnectionString();


        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] = postgresConnectionString,
                        ["ConnectionStrings:Redis"] = redisConnectionString,
                        ["ConnectionStrings:RabbitMq"] = "",
                        ["Hangfire:Disabled"] = "true",
                        ["Telemetry:Disabled"] = "true"
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
                        ConnectionMultiplexer.Connect(redisConnectionString));

                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
                    dataSourceBuilder.EnableDynamicJson();
                    var dataSource = dataSourceBuilder.Build();
                    services.RemoveAll<NpgsqlDataSource>();
                    services.RemoveAll<DbContextOptions<ECommerceDbContext>>();
                    services.RemoveAll<ECommerceDbContext>();
                    services.AddSingleton(dataSource);
                    services.AddScoped(sp =>
                    {
                        var builder = new DbContextOptionsBuilder<ECommerceDbContext>();
                        builder.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>());
                        builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                        builder.AddInterceptors(new DomainEventsInterceptor());
                        return new ECommerceDbContext(builder.Options);
                    });

                    services.RemoveAll<IHostedService>();
                });
            });

        await IntegrationFixture.EnsureDatabaseReadyAsync();

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
            for (int i = 0; i < 100; i++)
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

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
                    break;
                }

                Console.WriteLine($"Found {messages.Count} outbox messages");
                foreach (var message in messages)
                {
                    try
                    {
                        var domainEvent = Deserialize(message.EventType, message.Content);
                        Console.WriteLine($"Deserialized {message.EventType} to {domainEvent?.GetType().Name ?? "null"}");
                        if (domainEvent is null)
                        {
                            message.Attempts++;
                            message.Error = $"Unknown event type: {message.EventType}";
                            message.ProcessedOn = message.Attempts >= 5 ? DateTime.UtcNow : null;
                            continue;
                        }

                        await publisher.PublishAsync(message, domainEvent, CancellationToken.None);
                        Console.WriteLine($"Published {message.EventType}");
                        message.ProcessedOn = DateTime.UtcNow;
                        message.Error = null;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"Exception: {exception}");
                        message.Attempts++;
                        message.Error = exception.Message;
                        message.ProcessedOn = DateTime.UtcNow;
                    }
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            await postCommit.ExecuteAsync();
        });
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
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}

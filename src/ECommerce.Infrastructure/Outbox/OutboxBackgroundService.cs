using System.Text.Json;
using ECommerce.Domain.Abstractions;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Outbox;

public sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(configuration.GetValue("Outbox:PollingIntervalSeconds", 5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox processing failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
        var metrics = scope.ServiceProvider.GetRequiredService<OutboxMetrics>();
        var postCommit = scope.ServiceProvider.GetRequiredService<PostCommitActions>();

        var batchSize = configuration.GetValue("Outbox:BatchSize", 20);
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var messages = await dbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT * FROM "outbox_events"
                    WHERE "processed_on" IS NULL
                    ORDER BY "occurred_on"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            foreach (var message in messages)
            {
                await ProcessMessageAsync(publisher, metrics, message, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await postCommit.ExecuteAsync();
        });
    }

    private async Task ProcessMessageAsync(
        OutboxPublisher publisher,
        OutboxMetrics metrics,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var domainEvent = Deserialize(message.EventType, message.Content);
            if (domainEvent is null)
            {
                throw new InvalidOperationException($"Unknown outbox event type '{message.EventType}'.");
            }

            await publisher.PublishAsync(message, domainEvent, cancellationToken);
            message.ProcessedOn = DateTime.UtcNow;
            message.Error = null;
        }
        catch (Exception exception)
        {
            message.Attempts++;
            message.Error = exception.Message;
            message.ProcessedOn = message.Attempts >= MaxAttempts ? DateTime.UtcNow : null;

            if (message.Attempts >= MaxAttempts)
            {
                metrics.RecordDeadLetter();
                logger.LogError(
                    exception,
                    "Outbox message {OutboxMessageId} dead-lettered after {Attempt} attempts ({EventType}).",
                    message.Id,
                    message.Attempts,
                    message.EventType);
            }
            else
            {
                logger.LogError(
                    exception,
                    "Outbox message {OutboxMessageId} failed (attempt {Attempt})",
                    message.Id,
                    message.Attempts);
            }
        }
    }

    private static IDomainEvent? Deserialize(string eventType, string content)
    {
        var type = typeof(IDomainEvent).Assembly.GetType(eventType);

        return type is null
            ? null
            : JsonSerializer.Deserialize(content, type) as IDomainEvent;
    }
}

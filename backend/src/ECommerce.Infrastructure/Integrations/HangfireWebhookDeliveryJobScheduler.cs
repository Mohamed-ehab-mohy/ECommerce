using ECommerce.Infrastructure.Jobs;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Integrations.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integrations;

/// <summary>Enqueues or schedules a webhook delivery job into Hangfire (T-DAT-018).</summary>
public sealed class HangfireWebhookDeliveryJobScheduler(
    IBackgroundJobClient? backgroundJobClient,
    PostCommitActions? postCommitActions,
    ILogger<HangfireWebhookDeliveryJobScheduler> logger) : IWebhookDeliveryJobScheduler
{
    public void Enqueue(Guid deliveryId)
    {
        if (backgroundJobClient is null)
        {
            logger.LogInformation("Hangfire not configured; webhook {DeliveryId} will not be scheduled.", deliveryId);
            return;
        }

        if (postCommitActions is not null)
        {
            postCommitActions.Add(() =>
            {
                backgroundJobClient.Enqueue<DeliverWebhookJob>(job => job.ExecuteAsync(deliveryId, CancellationToken.None));
                return Task.CompletedTask;
            });
            return;
        }

        backgroundJobClient.Enqueue<DeliverWebhookJob>(job => job.ExecuteAsync(deliveryId, CancellationToken.None));
    }

    public void Schedule(Guid deliveryId, TimeSpan delay)
    {
        if (backgroundJobClient is null)
        {
            logger.LogInformation("Hangfire not configured; webhook {DeliveryId} will not be scheduled.", deliveryId);
            return;
        }

        backgroundJobClient.Schedule<DeliverWebhookJob>(
            job => job.ExecuteAsync(deliveryId, CancellationToken.None),
            delay);
    }
}

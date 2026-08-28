using ECommerce.UseCases.Integrations.Services;
using Hangfire;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>
/// Executes a single webhook delivery attempt and applies the retry/suspend policy.
/// Retries are scheduled by <see cref="WebhookDeliveryService"/>, so Hangfire automatic retries are off.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class DeliverWebhookJob(WebhookDeliveryService service)
{
    public Task ExecuteAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        service.DeliverAsync(deliveryId, cancellationToken);
}

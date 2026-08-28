using System.Text.Json;
using ECommerce.Domain.Integrations;
using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Ports;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Notifications.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.UseCases.Integrations.Services;

public sealed class WebhookOptions
{
    public const string SectionName = "Integrations:Webhooks";

    public int MaxAttempts { get; init; } = 5;

    public string OpsEmail { get; init; } = "ops@ecommerce.dev";
}

/// <summary>Outbound webhook envelope per docs/08 §8.2.</summary>
public sealed record WebhookEnvelope(
    string EventId,
    string Type,
    DateTime OccurredAt,
    string Version,
    object Payload);

/// <summary>
/// Orchestrates webhook dispatch and delivery: persists a pending delivery per subscribed endpoint,
/// POSTs signed payloads, and applies the retry/suspend policy.
/// </summary>
public sealed class WebhookDeliveryService(
    IWebhookEndpointRepository endpoints,
    IWebhookDeliveryRepository deliveries,
    IWebhookDeliveryJobScheduler scheduler,
    IWebhookSigner signer,
    IWebhookHttpDeliverer httpDeliverer,
    NotificationDispatcher notificationDispatcher,
    IOptions<WebhookOptions> options,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<WebhookDeliveryService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task DispatchAsync(
        DateTime occurredOn,
        string eventType,
        object payload,
        CancellationToken cancellationToken)
    {
        var subscribed = await endpoints.GetActiveByEventTypeAsync(eventType, cancellationToken);
        if (subscribed.Count == 0)
        {
            return;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var eventId = $"evt_{Guid.NewGuid():N}";
        var envelope = new WebhookEnvelope(eventId, eventType, occurredOn, "1.0", payload);
        var payloadJson = JsonSerializer.Serialize(envelope, JsonOptions);

        var created = new List<WebhookDelivery>(subscribed.Count);
        foreach (var endpoint in subscribed)
        {
            if (endpoint.IsSuspended(utcNow))
            {
                continue;
            }

            var delivery = WebhookDelivery.Create(endpoint.Id, eventId, eventType, payloadJson, utcNow);
            deliveries.Add(delivery);
            created.Add(delivery);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var delivery in created)
        {
            scheduler.Enqueue(delivery.Id);
        }

        logger.LogInformation(
            "Webhook {EventType} ({EventId}) queued for {Count} endpoint(s).",
            eventType,
            eventId,
            created.Count);
    }

    public async Task DeliverAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await deliveries.GetByIdAsync(deliveryId, cancellationToken);
        if (delivery is null)
        {
            logger.LogWarning("Webhook delivery {DeliveryId} not found; skipping.", deliveryId);
            return;
        }

        if (delivery.Status == WebhookDeliveryStatus.Delivered)
        {
            return;
        }

        var endpoint = await endpoints.GetByIdAsync(delivery.EndpointId, cancellationToken);
        if (endpoint is null || !endpoint.IsActive)
        {
            logger.LogWarning("Webhook endpoint for delivery {DeliveryId} is missing or inactive; skipping.", deliveryId);
            return;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (endpoint.IsSuspended(utcNow))
        {
            return;
        }

        var signature = signer.ComputeSignature(endpoint.Secret, delivery.PayloadJson);

        WebhookDeliveryResult result;
        try
        {
            result = await httpDeliverer.PostAsync(
                endpoint.Url,
                signature,
                delivery.EventId,
                delivery.EventType,
                delivery.PayloadJson,
                cancellationToken);
        }
        catch (Exception exception)
        {
            result = new WebhookDeliveryResult(false, null, exception.Message);
        }

        if (result.Success)
        {
            delivery.RecordSuccess(result.StatusCode ?? 200, utcNow);
            logger.LogInformation(
                "Webhook {DeliveryId} delivered to endpoint {EndpointId} ({StatusCode}).",
                delivery.Id,
                endpoint.Id,
                result.StatusCode);
        }
        else
        {
            await ApplyFailureAsync(endpoint, delivery, result, utcNow, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyFailureAsync(
        WebhookEndpoint endpoint,
        WebhookDelivery delivery,
        WebhookDeliveryResult result,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var attempts = delivery.Attempts + 1;
        var maxAttempts = options.Value.MaxAttempts;

        if (attempts >= maxAttempts)
        {
            delivery.Suspend(result.Error ?? "Webhook delivery failed.", utcNow);
            endpoint.Suspend(utcNow);
            await NotifySuspendedAsync(endpoint, delivery, utcNow, cancellationToken);
            logger.LogWarning(
                "Webhook endpoint {EndpointId} suspended after {Attempts} failed attempts.",
                endpoint.Id,
                attempts);
            return;
        }

        var delay = ComputeBackoff(attempts);
        delivery.RecordFailure(result.StatusCode, result.Error ?? "Webhook delivery failed.", utcNow + delay, utcNow);
        scheduler.Schedule(delivery.Id, delay);
        logger.LogWarning(
            "Webhook delivery {DeliveryId} failed ({StatusCode}); retrying in {Delay} (attempt {Attempts}/{MaxAttempts}).",
            delivery.Id,
            result.StatusCode,
            delay,
            attempts,
            maxAttempts);
    }

    private async Task NotifySuspendedAsync(
        WebhookEndpoint endpoint,
        WebhookDelivery delivery,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationDispatcher.DispatchAsync(new NotificationRequest(
                CustomerId: null,
                Channel: NotificationChannel.Email,
                Kind: NotificationKind.WebhookSuspended,
                TemplateKey: "integrations.webhook.suspended",
                Locale: "en",
                Recipient: options.Value.OpsEmail,
                ReferenceId: $"{endpoint.Id:N}",
                Placeholders: new Dictionary<string, string>
                {
                    ["EndpointName"] = endpoint.Name,
                    ["Url"] = endpoint.Url,
                    ["EventType"] = delivery.EventType,
                    ["SuspendedAt"] = utcNow.ToString("O")
                },
                Transactional: true), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to notify ops about suspended webhook endpoint {EndpointId}.", endpoint.Id);
        }
    }

    /// <summary>Exponential backoff: 1m, 2m, 4m, 8m for attempts 1..4 (docs/08 §8.1).</summary>
    private static TimeSpan ComputeBackoff(int attempt)
    {
        var minutes = Math.Min(1 << (attempt - 1), 8);
        return TimeSpan.FromMinutes(minutes);
    }
}

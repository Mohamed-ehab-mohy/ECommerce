using ECommerce.Domain.Integrations;

namespace ECommerce.UseCases.Integrations.Ports;

public interface IWebhookEndpointRepository
{
    Task<WebhookEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookEndpoint>> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookEndpoint>> ListAsync(CancellationToken cancellationToken);

    void Add(WebhookEndpoint endpoint);
}

public interface IWebhookDeliveryRepository
{
    Task<WebhookDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookDelivery>> ListByEndpointAsync(Guid endpointId, CancellationToken cancellationToken);

    void Add(WebhookDelivery delivery);
}

public interface IWebhookDeliveryJobScheduler
{
    void Enqueue(Guid deliveryId);

    void Schedule(Guid deliveryId, TimeSpan delay);
}

/// <summary>Computes the <c>X-Signature: sha256=...</c> value over the raw payload (docs/08 §8.1).</summary>
public interface IWebhookSigner
{
    string ComputeSignature(string secret, string payload);
}

public sealed record WebhookDeliveryResult(bool Success, int? StatusCode, string? Error);

/// <summary>POSTs a signed payload to a partner endpoint (T-DAT-018).</summary>
public interface IWebhookHttpDeliverer
{
    Task<WebhookDeliveryResult> PostAsync(
        string url,
        string signature,
        string eventId,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken);
}

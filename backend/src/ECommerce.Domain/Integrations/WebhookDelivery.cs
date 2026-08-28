using ECommerce.Domain.Common;

namespace ECommerce.Domain.Integrations;

public enum WebhookDeliveryStatus
{
    Pending,
    Delivered,
    Failed,
    Suspended
}

/// <summary>
/// A single outbound webhook delivery attempt log entry. Stores the signed
/// payload so it can be replayed unchanged, plus the retry/backoff bookkeeping.
/// </summary>
public sealed class WebhookDelivery : BaseEntity<Guid>
{
    private WebhookDelivery()
    {
        EventId = string.Empty;
        EventType = string.Empty;
        PayloadJson = string.Empty;
    }

    public Guid EndpointId { get; private set; }

    /// <summary>Opaque event id (<c>evt_...</c>) delivered in the envelope and headers.</summary>
    public string EventId { get; private set; }

    public string EventType { get; private set; }

    /// <summary>Serialized envelope (eventId/type/occurredAt/version/payload) delivered to the endpoint.</summary>
    public string PayloadJson { get; private set; }

    public WebhookDeliveryStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? NextRetryAtUtc { get; private set; }

    public int? LastStatusCode { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public static WebhookDelivery Create(
        Guid endpointId,
        string eventId,
        string eventType,
        string payloadJson,
        DateTime utcNow)
    {
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            EndpointId = endpointId,
            EventId = eventId,
            EventType = eventType,
            PayloadJson = payloadJson,
            Status = WebhookDeliveryStatus.Pending,
            Attempts = 0,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void RecordSuccess(int statusCode, DateTime utcNow)
    {
        Status = WebhookDeliveryStatus.Delivered;
        Attempts++;
        LastStatusCode = statusCode;
        LastError = null;
        NextRetryAtUtc = null;
        DeliveredAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Records a failed attempt; when <paramref name="nextRetryAtUtc"/> is null no retry is scheduled.</summary>
    public void RecordFailure(int? statusCode, string error, DateTime? nextRetryAtUtc, DateTime utcNow)
    {
        Attempts++;
        LastStatusCode = statusCode;
        LastError = error;
        NextRetryAtUtc = nextRetryAtUtc;
        Status = nextRetryAtUtc is null ? WebhookDeliveryStatus.Failed : WebhookDeliveryStatus.Pending;
        DeliveredAtUtc = null;
        UpdatedAt = utcNow;
    }

    public void Suspend(string error, DateTime utcNow)
    {
        Status = WebhookDeliveryStatus.Suspended;
        LastError = error;
        NextRetryAtUtc = null;
        UpdatedAt = utcNow;
    }

    /// <summary>Rewinds the delivery to Pending so replay re-runs it from attempt 0 (docs/08 §8.1).</summary>
    public void ResetForReplay(DateTime utcNow)
    {
        Status = WebhookDeliveryStatus.Pending;
        Attempts = 0;
        LastStatusCode = null;
        LastError = null;
        NextRetryAtUtc = null;
        DeliveredAtUtc = null;
        UpdatedAt = utcNow;
    }
}

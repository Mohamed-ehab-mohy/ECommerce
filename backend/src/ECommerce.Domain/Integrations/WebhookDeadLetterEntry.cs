using ECommerce.Domain.Common;

namespace ECommerce.Domain.Integrations;

public sealed class WebhookDeadLetterEntry : BaseEntity<Guid>
{
    private WebhookDeadLetterEntry()
    {
        EventType = string.Empty;
        EventId = string.Empty;
        PayloadJson = string.Empty;
        EndpointUrl = string.Empty;
        EndpointName = string.Empty;
        ErrorReason = string.Empty;
    }

    public Guid DeliveryId { get; private set; }

    public Guid EndpointId { get; private set; }

    public string EventType { get; private set; }

    public string EventId { get; private set; }

    public string PayloadJson { get; private set; }

    public string EndpointUrl { get; private set; }

    public string EndpointName { get; private set; }

    public int TotalAttempts { get; private set; }

    public int? LastStatusCode { get; private set; }

    public string ErrorReason { get; private set; }

    public DateTime FirstFailedAtUtc { get; private set; }

    public DateTime LastFailedAtUtc { get; private set; }

    public DateTime? ReplayedAtUtc { get; private set; }

    public bool IsReplayed => ReplayedAtUtc.HasValue;

    public static WebhookDeadLetterEntry Create(
        Guid deliveryId,
        Guid endpointId,
        string eventType,
        string eventId,
        string payloadJson,
        string endpointUrl,
        string endpointName,
        int totalAttempts,
        int? lastStatusCode,
        string errorReason,
        DateTime utcNow)
    {
        return new WebhookDeadLetterEntry
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            EndpointId = endpointId,
            EventType = eventType,
            EventId = eventId,
            PayloadJson = payloadJson,
            EndpointUrl = endpointUrl,
            EndpointName = endpointName,
            TotalAttempts = totalAttempts,
            LastStatusCode = lastStatusCode,
            ErrorReason = errorReason,
            FirstFailedAtUtc = utcNow,
            LastFailedAtUtc = utcNow,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void MarkReplayed(DateTime utcNow)
    {
        ReplayedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }
}

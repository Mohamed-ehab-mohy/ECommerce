using ECommerce.Domain.Common;

namespace ECommerce.Domain.Integrations;

/// <summary>
/// A partner webhook endpoint (US-M-004, T-DAT-018). Holds the HMAC signing secret and the
/// subscribed event types; deliveries are recorded separately in <see cref="WebhookDelivery"/>.
/// </summary>
public sealed class WebhookEndpoint : BaseEntity<Guid>
{
    private readonly List<string> _eventTypes = [];

    private WebhookEndpoint()
    {
        Name = string.Empty;
        Url = string.Empty;
        Secret = string.Empty;
    }

    public string Name { get; private set; }

    public string Url { get; private set; }

    /// <summary>HMAC-SHA256 signing secret. Returned only on create/rotate; never listed again.</summary>
    public string Secret { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>While set and in the future the endpoint is temporarily suspended (T-DAT-018).</summary>
    public DateTime? SuspendedUntilUtc { get; private set; }

    public DateTime? SecretRotatedAtUtc { get; private set; }

    public IReadOnlyCollection<string> EventTypes => _eventTypes;

    public static WebhookEndpoint Create(
        string name,
        string url,
        string secret,
        IReadOnlyCollection<string> eventTypes,
        DateTime utcNow)
    {
        var endpoint = new WebhookEndpoint
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = url,
            Secret = secret,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        endpoint._eventTypes.AddRange(eventTypes.Distinct(StringComparer.Ordinal));

        return endpoint;
    }

    public void RotateSecret(string secret, DateTime utcNow)
    {
        Secret = secret;
        SecretRotatedAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public bool IsSubscribedTo(string eventType) =>
        IsActive && _eventTypes.Contains(eventType, StringComparer.Ordinal);

    public bool IsSuspended(DateTime utcNow) =>
        SuspendedUntilUtc is { } until && utcNow < until;

    public void Suspend(DateTime utcNow)
    {
        if (IsSuspended(utcNow))
        {
            return;
        }

        SuspendedUntilUtc = utcNow.AddHours(1);
        UpdatedAt = utcNow;
    }

    public void Resume(DateTime utcNow)
    {
        SuspendedUntilUtc = null;
        IsActive = true;
        UpdatedAt = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }
}

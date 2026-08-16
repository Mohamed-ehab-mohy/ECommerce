namespace ECommerce.UseCases.Integrations.Responses;

public sealed record WebhookEndpointResponse(
    Guid EndpointId,
    string Name,
    string Url,
    bool IsActive,
    DateTime? SuspendedUntilUtc,
    IReadOnlyList<string> EventTypes);

public sealed record WebhookEndpointCreatedResponse(
    Guid EndpointId,
    string Name,
    string Url,
    string Secret,
    IReadOnlyList<string> EventTypes);

public sealed record WebhookSecretRotatedResponse(Guid EndpointId, string Secret);

public sealed record WebhookReplayResponse(int Replayed);

public sealed record WebhookDeliveryResponse(
    Guid DeliveryId,
    Guid EndpointId,
    string EventId,
    string EventType,
    string Status,
    int Attempts,
    int? LastStatusCode,
    string? LastError,
    DateTime? NextRetryAtUtc,
    DateTime? DeliveredAtUtc);

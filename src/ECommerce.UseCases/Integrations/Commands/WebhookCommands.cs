using ECommerce.Domain.Integrations;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Responses;

namespace ECommerce.UseCases.Integrations.Commands;

/// <summary>Registers a partner webhook endpoint and returns the signing secret once (US-M-004).</summary>
public sealed record CreateWebhookEndpointCommand(
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes)
    : IRequest<Result<WebhookEndpointCreatedResponse>>, IRequirePermission
{
    public string Permission => Permissions.IntegrationsWrite;
}

/// <summary>Rotates the endpoint secret; the new secret is returned once (docs/08 §6.10).</summary>
public sealed record RotateWebhookSecretCommand(Guid EndpointId)
    : IRequest<Result<WebhookSecretRotatedResponse>>, IRequirePermission
{
    public string Permission => Permissions.IntegrationsWrite;
}

/// <summary>Replays one or all failed deliveries for an endpoint (docs/08 §8.1).</summary>
public sealed record ReplayWebhookCommand(Guid EndpointId, Guid? DeliveryId)
    : IRequest<Result<WebhookReplayResponse>>, IRequirePermission
{
    public string Permission => Permissions.IntegrationsWrite;
}

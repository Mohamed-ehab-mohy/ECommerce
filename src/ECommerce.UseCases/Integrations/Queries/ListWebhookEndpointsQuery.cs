using ECommerce.Domain.Integrations;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Responses;

namespace ECommerce.UseCases.Integrations.Queries;

/// <summary>Lists registered webhook endpoints (docs/08 §6.10, integrations.read).</summary>
public sealed record ListWebhookEndpointsQuery()
    : IRequest<Result<IReadOnlyList<WebhookEndpointResponse>>>, IRequirePermission
{
    public string Permission => Permissions.IntegrationsRead;
}

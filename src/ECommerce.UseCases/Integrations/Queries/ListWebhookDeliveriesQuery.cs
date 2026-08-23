using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Responses;

namespace ECommerce.UseCases.Integrations.Queries;

/// <summary>Returns the delivery log for an endpoint (T-DAT-018, docs/08 §8.1).</summary>
public sealed record ListWebhookDeliveriesQuery(Guid EndpointId, int? Limit)
    : IRequest<Result<IReadOnlyList<WebhookDeliveryResponse>>>, IRequirePermission
{
    public string Permission => Permissions.IntegrationsRead;
}

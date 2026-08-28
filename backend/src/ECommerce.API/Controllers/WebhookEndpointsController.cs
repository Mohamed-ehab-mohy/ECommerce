using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Commands;
using ECommerce.UseCases.Integrations.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/webhook-endpoints")]
public sealed class WebhookEndpointsController(ISender sender) : ControllerBase
{
    /// <summary>Registers a webhook endpoint; returns the signing secret once (integrations.write).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookEndpointRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateWebhookEndpointCommand(request.Name, request.Url, request.EventTypes),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : CreatedAtAction(nameof(Create), new { id = result.Value.EndpointId }, result.Value);
    }

    /// <summary>Lists registered webhook endpoints (integrations.read).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListWebhookEndpointsQuery(), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Rotates the endpoint secret; returns the new secret once (integrations.write, docs/08 §6.10).</summary>
    [HttpPost("{endpointId:guid}/rotate-secret")]
    public async Task<IActionResult> RotateSecret(Guid endpointId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RotateWebhookSecretCommand(endpointId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Returns the delivery log for an endpoint (integrations.read).</summary>
    [HttpGet("{endpointId:guid}/deliveries")]
    public async Task<IActionResult> Deliveries(
        Guid endpointId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListWebhookDeliveriesQuery(endpointId, limit),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Replays one or all failed deliveries for an endpoint (integrations.write, docs/08 §8.1).</summary>
    [HttpPost("{endpointId:guid}/replay")]
    public async Task<IActionResult> Replay(
        Guid endpointId,
        [FromBody] ReplayWebhookRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReplayWebhookCommand(endpointId, request?.DeliveryId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

public sealed record CreateWebhookEndpointRequest(
    string Name,
    string Url,
    IReadOnlyList<string> EventTypes);

public sealed record ReplayWebhookRequest(Guid EndpointId, Guid? DeliveryId);

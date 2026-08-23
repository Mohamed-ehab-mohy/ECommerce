using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Commands;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/webhooks")]
public sealed class WebhooksController(ISender sender) : ControllerBase
{
    /// <summary>Replays one or all failed/suspended deliveries for an endpoint (integrations.write, docs/08 §8.1).</summary>
    [HttpPost("replay")]
    public async Task<IActionResult> Replay([FromBody] ReplayWebhookRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReplayWebhookCommand(request.EndpointId, request.DeliveryId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

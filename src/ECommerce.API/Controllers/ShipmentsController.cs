using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/shipments")]
public sealed class ShipmentsController(ISender sender) : ControllerBase
{
    [HttpGet("{shipmentId:guid}")]
    public async Task<IActionResult> Get(Guid shipmentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetShipmentQuery(shipmentId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("{shipmentId:guid}/tracking")]
    public async Task<IActionResult> ApplyTracking(
        Guid shipmentId,
        ApplyShipmentTrackingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApplyShipmentTrackingCommand(shipmentId, request.Status), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/warehouses")]
public sealed class WarehousesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListWarehousesQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{warehouseId:guid}")]
    public async Task<IActionResult> Get(Guid warehouseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWarehouseQuery(warehouseId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateWarehouseCommand(
            request.Code,
            request.Name,
            request.Address,
            request.Timezone,
            request.Status), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [HttpPatch("{warehouseId:guid}")]
    public async Task<IActionResult> Update(Guid warehouseId, UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateWarehouseCommand(
            warehouseId,
            request.Name,
            request.Address,
            request.Timezone,
            request.Status), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [HttpDelete("{warehouseId:guid}")]
    public async Task<IActionResult> Deactivate(Guid warehouseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateWarehouseCommand(warehouseId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

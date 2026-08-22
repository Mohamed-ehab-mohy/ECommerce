using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/stock")]
public sealed class StockController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListStockItemsQuery(page, pageSize, warehouseId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{stockItemId:guid}")]
    public async Task<IActionResult> Get(Guid stockItemId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStockItemQuery(stockItemId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("movements")]
    public async Task<IActionResult> ListMovements(
        [FromQuery] Guid stockItemId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListStockMovementsQuery(stockItemId, page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("movements")]
    public async Task<IActionResult> PostMovement(PostStockMovementRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PostStockMovementCommand(
            request.Sku,
            request.WarehouseId,
            request.Type,
            request.Quantity,
            request.Reason,
            request.Reference,
            request.Note,
            request.ApprovedBy), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> Transfer(TransferStockRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new TransferStockCommand(
            request.Sku,
            request.FromWarehouseId,
            request.ToWarehouseId,
            request.Quantity,
            request.Note), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

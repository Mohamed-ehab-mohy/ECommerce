using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/fulfillment")]
public sealed class FulfillmentController(ISender sender) : ControllerBase
{
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask(CreateFulfillmentTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateFulfillmentTaskCommand(
            request.OrderId,
            request.WarehouseId,
            request.Priority,
            request.Zone), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : CreatedAtAction(nameof(GetTask), new { taskId = result.Value.TaskId }, result.Value);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> ListTasks(
        [FromQuery] Guid? warehouseId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListFulfillmentQueueQuery(warehouseId, status, page, pageSize), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<IActionResult> GetTask(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFulfillmentTaskQuery(taskId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("tasks/{taskId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid taskId, AssignFulfillmentTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignFulfillmentTaskCommand(taskId, request.AssigneeId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("tasks/{taskId:guid}/start-picking")]
    public async Task<IActionResult> StartPicking(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartPickingFulfillmentTaskCommand(taskId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("tasks/{taskId:guid}/split")]
    public async Task<IActionResult> Split(Guid taskId, SplitFulfillmentTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SplitFulfillmentTaskCommand(
            taskId,
            request.WarehouseId,
            request.ItemIds,
            request.Priority,
            request.Zone), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPut("orders/{orderId:guid}/shipping-address")]
    public async Task<IActionResult> CorrectShippingAddress(
        Guid orderId,
        CorrectShippingAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CorrectShippingAddressCommand(
            orderId,
            request.FullName,
            request.Phone,
            request.Street,
            request.City,
            request.Region,
            request.Country,
            request.PostalCode), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : NoContent();
    }

    [HttpPost("tasks/{taskId:guid}/pack")]
    public async Task<IActionResult> Pack(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkFulfillmentTaskPackedCommand(taskId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("pick-lists")]
    public async Task<IActionResult> GetPickLists([FromQuery] Guid warehouseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPickListQuery(warehouseId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("shipments")]
    public async Task<IActionResult> CreateShipment(CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateShipmentCommand(
            request.TaskId,
            request.CarrierKey,
            request.DestinationCountry,
            request.DestinationPostalCode,
            request.WeightGrams,
            request.Currency), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("shipping-rates/quote")]
    public async Task<IActionResult> QuoteShippingRate(
        [FromQuery] string destinationCountry,
        [FromQuery] string destinationPostalCode,
        [FromQuery] int weightGrams,
        [FromQuery] string currency,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new QuoteShippingRateQuery(
            destinationCountry,
            destinationPostalCode,
            weightGrams,
            currency), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

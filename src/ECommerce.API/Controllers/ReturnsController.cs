using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Commands;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/returns")]
public sealed class ReturnsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateReturnRequestCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { }, cancellationToken);
        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("{returnId:guid}")]
    public async Task<IActionResult> Get(Guid returnId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReturnRequestQuery(returnId), cancellationToken);
        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> ListByOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListReturnRequestsByOrderQuery(orderId), cancellationToken);
        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{returnId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid returnId, ApproveReturnRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveReturnRequestCommand(returnId, request.Notes), cancellationToken);
        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(new { returnId, status = "approved" });
    }

    [Authorize]
    [HttpPost("{returnId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid returnId, RejectReturnRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RejectReturnRequestCommand(returnId, request.Reason), cancellationToken);
        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(new { returnId, status = "rejected" });
    }
}

public sealed record ApproveReturnRequestRequest(string? Notes);
public sealed record RejectReturnRequestRequest(string Reason);

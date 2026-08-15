using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class RefundsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("orders/{orderNumber}/refunds")]
    public async Task<IActionResult> RequestRefund(
        string orderNumber,
        CreateRefundRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RequestRefundCommand(
                orderNumber,
                request.Amount,
                request.Reason,
                request.Items,
                request.Restock,
                idempotencyKey ?? string.Empty),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Created($"/api/v1/refunds/{result.Value.RefundId}", result.Value);
    }

    [HttpPost("refunds/{refundId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid refundId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ApproveRefundCommand(refundId, currentUser.UserId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("refunds/{refundId:guid}/execute")]
    public async Task<IActionResult> Execute(Guid refundId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ExecuteRefundCommand(refundId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

public sealed record CreateRefundRequest(
    decimal Amount,
    string Reason,
    IReadOnlyCollection<RefundItemRequest>? Items = null,
    bool Restock = false);

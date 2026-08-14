using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost("{paymentId:guid}/authorize")]
    public async Task<IActionResult> Authorize(Guid paymentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AuthorizePaymentCommand(paymentId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("{paymentId:guid}/refund")]
    public async Task<IActionResult> RequestRefund(Guid paymentId, RefundRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RequestRefundCommand(paymentId, request.Reason),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("{paymentId:guid}/refund/complete")]
    public async Task<IActionResult> CompleteRefund(Guid paymentId, CompleteRefundRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteRefundCommand(paymentId, request.ProviderReference),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

public sealed record RefundRequest(string Reason);

public sealed record CompleteRefundRequest(string? ProviderReference = null);

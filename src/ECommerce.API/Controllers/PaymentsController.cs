using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;

using ECommerce.API.Filters;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Idempotent]
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
}

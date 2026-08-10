using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/support/orders")]
public sealed class SupportOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? orderNumber,
        [FromQuery] string? email,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SupportOrderLookupQuery(orderNumber, email, customerId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

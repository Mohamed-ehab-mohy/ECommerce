using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? email = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchCustomersQuery(email, page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerQuery(customerId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

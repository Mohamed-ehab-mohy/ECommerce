using ECommerce.API.Common;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/brands")]
public sealed class BrandsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 900)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListBrandsQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBrandCommand(
            request.Name,
            request.Description,
            request.Website), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [Authorize]
    [HttpPatch("{brandId:guid}")]
    public async Task<IActionResult> Update(Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBrandCommand(
            brandId,
            request.Name,
            request.Description,
            request.Website), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

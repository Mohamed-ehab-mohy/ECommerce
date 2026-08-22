using ECommerce.API.Common;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 900)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCategoryTreeQuery(), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCategoryCommand(
            request.Name,
            request.Slug,
            request.ParentId,
            request.SortOrder), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [Authorize]
    [HttpPatch("{categoryId:guid}")]
    public async Task<IActionResult> Update(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateCategoryCommand(
            categoryId,
            request.Name,
            request.Slug,
            request.ParentId,
            request.SortOrder), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

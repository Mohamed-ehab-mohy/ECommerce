using ECommerce.API.Common;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/partner")]
public sealed class PartnersController(ISender sender) : ControllerBase
{
    [HttpGet("catalog/products")]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? locale = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListProductsQuery(page, pageSize, locale, currency), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("catalog/products/{id:guid}")]
    public async Task<IActionResult> GetProduct(
        Guid id,
        [FromQuery] string? locale = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductQuery(id, locale, currency), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

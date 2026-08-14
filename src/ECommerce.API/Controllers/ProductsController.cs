using ECommerce.API.Common;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery(Name = "price.gte")] decimal? priceGte = null,
        [FromQuery(Name = "price.lte")] decimal? priceLte = null,
        [FromQuery(Name = "rating.gte")] decimal? ratingGte = null,
        [FromQuery] string? locale = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(q)
            || categoryId is not null
            || brandId is not null
            || priceGte is not null
            || priceLte is not null
            || ratingGte is not null;

        if (hasSearch)
        {
            var result = await sender.Send(
                new SearchProductsQuery(
                    q, categoryId, brandId, priceGte, priceLte, ratingGte, page, pageSize, locale, currency),
                cancellationToken);

            return result.IsFailure
                ? ToProblem(result.ToOperationError())
                : Ok(result.Value);
        }

        var listResult = await sender.Send(new ListProductsQuery(page, pageSize, locale, currency), cancellationToken);

        return listResult.IsFailure
            ? ToProblem(listResult.ToOperationError())
            : Ok(listResult.Value);
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(
        Guid productId,
        [FromQuery] string? locale = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductQuery(productId, locale, currency), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProductCommand(
            request.Sku,
            request.Slug,
            request.Name,
            request.Description,
            request.Currency,
            request.ListAmount,
            request.OfferAmount,
            request.CategoryId,
            request.BrandId,
            request.IsFeatured,
            request.Status,
            request.Locale), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [Authorize]
    [HttpPatch("{productId:guid}")]
    public async Task<IActionResult> Update(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProductCommand(
            productId,
            request.Slug,
            request.Name,
            request.Description,
            request.Currency,
            request.ListAmount,
            request.OfferAmount,
            request.CategoryId,
            request.BrandId,
            request.IsFeatured,
            request.Status,
            request.Locale), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Deactivate(Guid productId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateProductCommand(productId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

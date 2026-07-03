using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

public class ProductsController : ApiControllerBase
{
    private readonly IProductQueryService _queryService;

    public ProductsController(IProductQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetAllProductsResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetAllProductsResponse>>> GetAll(CancellationToken ct = default)
    {
        var products = await _queryService.GetAllProductsAsync(ct);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetByIdProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetByIdProductResponse>> GetById(Guid id, CancellationToken ct = default)
    {
        var product = await _queryService.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound();

        return Ok(product);
    }
}

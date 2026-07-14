using Asp.Versioning;
using ECommerce.API.Models;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiVersion("1")]
public class ProductsController : ApiControllerBase
{
    private readonly IProductQueryService _queryService;

    public ProductsController(IProductQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>
    /// Get all products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GetAllProductsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var products = await _queryService.GetAllProductsAsync(ct);
        return Success(products);
    }

    /// <summary>
    /// Get a product by its identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetByIdProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var product = await _queryService.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound();

        return Success(product);
    }
}

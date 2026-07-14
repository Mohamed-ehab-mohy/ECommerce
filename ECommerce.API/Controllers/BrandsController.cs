using Asp.Versioning;
using ECommerce.API.Models;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiVersion("1")]
public class BrandsController : ApiControllerBase
{
    private readonly IBrandQueryService _queryService;

    public BrandsController(IBrandQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>
    /// Get all brands.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GetAllBrandsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var brands = await _queryService.GetAllAsync(ct);
        return Success(brands);
    }
}

using ECommerce.API.Models;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

public class BrandsController : ApiControllerBase
{
    private readonly IBrandQueryService _queryService;

    public BrandsController(IBrandQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<GetAllBrandsResponse>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var brands = await _queryService.GetAllAsync(ct);
        return Success(brands);
    }
}

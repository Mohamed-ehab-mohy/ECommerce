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
    public async Task<ActionResult<IReadOnlyList<GetAllBrandsResponse>>> GetAll(CancellationToken ct = default)
    {
        var brands = await _queryService.GetAllAsync(ct);
        return Ok(brands);
    }
}

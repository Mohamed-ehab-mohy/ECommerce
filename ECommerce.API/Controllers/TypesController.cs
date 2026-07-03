using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

public class TypesController : ApiControllerBase
{
    private readonly ITypeQueryService _queryService;

    public TypesController(ITypeQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GetAllTypesResponse>>> GetAll(CancellationToken ct = default)
    {
        var types = await _queryService.GetAllAsync(ct);
        return Ok(types);
    }
}

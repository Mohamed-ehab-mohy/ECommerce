using Asp.Versioning;
using ECommerce.API.Models;
using ECommerce.UseCases.Products;
using ECommerce.UseCases.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiVersion("1")]
public class TypesController : ApiControllerBase
{
    private readonly ITypeQueryService _queryService;

    public TypesController(ITypeQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>
    /// Get all types.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GetAllTypesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var types = await _queryService.GetAllAsync(ct);
        return Success(types);
    }
}

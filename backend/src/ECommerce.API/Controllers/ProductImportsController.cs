using ECommerce.API.Common;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/imports")]
public sealed class ProductImportsController(ISender sender) : ControllerBase
{
    /// <summary>Starts an async bulk product import (catalog.product.write).</summary>
    [HttpPost("products")]
    public async Task<IActionResult> Start([FromBody] StartProductImportRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new StartProductImportCommand(request.Rows),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Accepted($"/api/v1/imports/{result.Value.ImportId}", result.Value);
    }

    /// <summary>Returns the import status and per-row error report (catalog.product.write).</summary>
    [HttpGet("{importId:guid}")]
    public async Task<IActionResult> Get(Guid importId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductImportQuery(importId), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

public sealed record StartProductImportRequest(IReadOnlyList<ProductImportRow> Rows);

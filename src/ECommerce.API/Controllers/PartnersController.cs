using ECommerce.API.Common;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Partners;
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

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreatePartnerAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePartnerAccountCommand(request.Name, request.Email, request.RateLimitPerMinute),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Created($"/api/v1/partner/accounts/{result.Value.Id}", result.Value);
    }

    [HttpPost("accounts/{partnerId:guid}/api-keys")]
    public async Task<IActionResult> CreateApiKey(
        Guid partnerId,
        [FromBody] CreatePartnerApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePartnerApiKeyCommand(partnerId, request),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Created($"/api/v1/partner/api-keys/{result.Value.Id}", result.Value);
    }

    [HttpGet("accounts/{partnerId:guid}/api-keys")]
    public async Task<IActionResult> ListApiKeys(
        Guid partnerId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListPartnerApiKeysQuery(partnerId),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("api-keys/{apiKeyId:guid}/revoke")]
    public async Task<IActionResult> RevokeApiKey(
        Guid apiKeyId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RevokePartnerApiKeyCommand(apiKeyId),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

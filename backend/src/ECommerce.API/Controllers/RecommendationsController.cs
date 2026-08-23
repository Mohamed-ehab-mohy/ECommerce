using ECommerce.UseCases.Common;
using ECommerce.UseCases.Recommendations;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/recommendations")]
public sealed class RecommendationsController(IRecommendationService recommendationService) : ControllerBase
{
    [HttpGet("for-me")]
    [Authorize]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetPersonalizedRecommendations(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var recommendations = await recommendationService.GetRecommendationsForUserAsync(userId.Value, limit, cancellationToken);
        return Ok(recommendations);
    }

    [HttpGet("bought-together/{productId:guid}")]
    [ResponseCache(Duration = 600)]
    public async Task<IActionResult> GetFrequentlyBoughtTogether(
        Guid productId,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await recommendationService.GetFrequentlyBoughtTogetherAsync(productId, limit, cancellationToken);
        return Ok(recommendations);
    }

    [HttpGet("trending")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetTrendingProducts(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await recommendationService.GetTrendingProductsAsync(limit, cancellationToken);
        return Ok(recommendations);
    }

    private Guid? GetUserId()
    {
        var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }
}

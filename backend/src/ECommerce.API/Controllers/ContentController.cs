using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace ECommerce.API.Controllers;

public sealed record CreateBannerRequest(
    Guid? TenantId,
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive);

[ApiController]
[Route("api/v1/content")]
public sealed class ContentController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost("banners")]
    public async Task<IActionResult> CreateBanner(CreateBannerRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBannerCommand(
            request.TenantId,
            request.Title,
            request.ImageUrl,
            request.TargetUrl,
            request.DisplayOrder,
            request.IsActive), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

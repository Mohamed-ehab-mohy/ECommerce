using ECommerce.API.Common;
using ECommerce.Shared.Content;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using MediatR;

namespace ECommerce.API.Controllers;

public sealed record CreateBannerRequest(
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive);

public sealed record UpdateBannerRequest(
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive);

public sealed record CreatePageRequest(
    string Title,
    string Slug,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    bool IsPublished);

public sealed record UpdatePageRequest(
    string Title,
    string Slug,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    bool IsPublished);

public sealed record LayoutSectionRequest(
    string Title,
    CmsSectionType Type,
    int DisplayOrder,
    string? ConfigJson,
    bool IsActive);

public sealed record CreateLayoutRequest(
    string Name,
    string Slug,
    bool IsActive,
    IReadOnlyList<LayoutSectionRequest> Sections);

public sealed record UpdateLayoutRequest(
    string Name,
    string Slug,
    bool IsActive,
    IReadOnlyList<LayoutSectionRequest> Sections);

[ApiController]
[Route("api/v1/content")]
public sealed class ContentController(ISender sender) : ControllerBase
{
    [HttpGet("banners")]
    [OutputCache(Duration = 60, VaryByQueryKeys = ["page", "pageSize"])]
    public async Task<IActionResult> ListBanners(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListBannersQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("pages/{slug}")]
    [OutputCache(Duration = 300)]
    public async Task<IActionResult> GetPageBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPageBySlugQuery(slug), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("layouts/{slug}")]
    [OutputCache(Duration = 300)]
    public async Task<IActionResult> GetLayoutBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCmsLayoutBySlugQuery(slug), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpGet("admin/banners")]
    public async Task<IActionResult> ListBannersAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AdminListBannersQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpGet("admin/banners/{bannerId:guid}")]
    public async Task<IActionResult> GetBanner(Guid bannerId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBannerQuery(bannerId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("admin/banners")]
    public async Task<IActionResult> CreateBanner(CreateBannerRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBannerCommand(
            TenantId: null,
            request.Title,
            request.ImageUrl,
            request.TargetUrl,
            request.DisplayOrder,
            request.IsActive), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [Authorize]
    [HttpPatch("admin/banners/{bannerId:guid}")]
    public async Task<IActionResult> UpdateBanner(Guid bannerId, UpdateBannerRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBannerCommand(
            bannerId,
            request.Title,
            request.ImageUrl,
            request.TargetUrl,
            request.DisplayOrder,
            request.IsActive), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpDelete("admin/banners/{bannerId:guid}")]
    public async Task<IActionResult> DeactivateBanner(Guid bannerId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateBannerCommand(bannerId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpGet("admin/pages")]
    public async Task<IActionResult> ListPagesAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AdminListPagesQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpGet("admin/pages/{pageId:guid}")]
    public async Task<IActionResult> GetPage(Guid pageId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPageQuery(pageId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("admin/pages")]
    public async Task<IActionResult> CreatePage(CreatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePageCommand(
            TenantId: null,
            request.Title,
            request.Slug,
            request.HtmlContent,
            request.MetaTitle,
            request.MetaDescription,
            request.IsPublished), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [Authorize]
    [HttpPatch("admin/pages/{pageId:guid}")]
    public async Task<IActionResult> UpdatePage(Guid pageId, UpdatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePageCommand(
            pageId,
            request.Title,
            request.Slug,
            request.HtmlContent,
            request.MetaTitle,
            request.MetaDescription,
            request.IsPublished), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpDelete("admin/pages/{pageId:guid}")]
    public async Task<IActionResult> DeactivatePage(Guid pageId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivatePageCommand(pageId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpGet("admin/layouts")]
    public async Task<IActionResult> ListLayoutsAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new AdminListCmsLayoutsQuery(page, pageSize), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpGet("admin/layouts/{layoutId:guid}")]
    public async Task<IActionResult> GetLayout(Guid layoutId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCmsLayoutQuery(layoutId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("admin/layouts")]
    public async Task<IActionResult> CreateLayout(CreateLayoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCmsLayoutCommand(
            TenantId: null,
            request.Name,
            request.Slug,
            request.IsActive,
            request.Sections.Select(section => new CmsLayoutSectionInput(
                section.Title,
                section.Type,
                section.DisplayOrder,
                section.ConfigJson,
                section.IsActive)).ToList()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [Authorize]
    [HttpPatch("admin/layouts/{layoutId:guid}")]
    public async Task<IActionResult> UpdateLayout(Guid layoutId, UpdateLayoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateCmsLayoutCommand(
            layoutId,
            request.Name,
            request.Slug,
            request.IsActive,
            request.Sections.Select(section => new CmsLayoutSectionInput(
                section.Title,
                section.Type,
                section.DisplayOrder,
                section.ConfigJson,
                section.IsActive)).ToList()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpDelete("admin/layouts/{layoutId:guid}")]
    public async Task<IActionResult> DeactivateLayout(Guid layoutId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateCmsLayoutCommand(layoutId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

using ECommerce.Shared.Content;

namespace ECommerce.UseCases.Content.Responses;

public sealed record CmsLayoutSectionResponse(
    Guid Id,
    string Title,
    CmsSectionType Type,
    int DisplayOrder,
    string? ConfigJson,
    bool IsActive);

public sealed record CmsLayoutResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    IReadOnlyList<CmsLayoutSectionResponse> Sections);

public sealed record PagedCmsLayoutsResponse(
    IReadOnlyList<CmsLayoutResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

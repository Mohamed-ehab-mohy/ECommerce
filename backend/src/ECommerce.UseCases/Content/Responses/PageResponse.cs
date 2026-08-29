namespace ECommerce.UseCases.Content.Responses;

public sealed record PageResponse(
    Guid Id,
    string Title,
    string Slug,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    bool IsPublished);

public sealed record PagedPagesResponse(
    IReadOnlyList<PageResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

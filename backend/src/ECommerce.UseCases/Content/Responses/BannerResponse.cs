namespace ECommerce.UseCases.Content.Responses;

public sealed record BannerResponse(
    Guid Id,
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive);

public sealed record PagedBannersResponse(
    IReadOnlyList<BannerResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

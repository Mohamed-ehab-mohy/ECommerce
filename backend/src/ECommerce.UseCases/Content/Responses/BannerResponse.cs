namespace ECommerce.UseCases.Content.Responses;

public sealed record BannerResponse(
    Guid Id,
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive);

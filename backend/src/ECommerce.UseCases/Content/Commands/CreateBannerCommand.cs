using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Commands;

public sealed record CreateBannerCommand(
    Guid? TenantId,
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive) : IRequest<Result<BannerResponse>>;

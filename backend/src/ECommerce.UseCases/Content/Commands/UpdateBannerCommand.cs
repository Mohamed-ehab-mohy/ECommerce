using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Content.Commands;

public sealed record UpdateBannerCommand(
    Guid BannerId,
    string Title,
    string ImageUrl,
    string? TargetUrl,
    int DisplayOrder,
    bool IsActive) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.ContentBannerWrite;
}

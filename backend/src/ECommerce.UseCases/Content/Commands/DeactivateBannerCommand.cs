using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Content.Commands;

public sealed record DeactivateBannerCommand(Guid BannerId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.ContentBannerDelete;
}

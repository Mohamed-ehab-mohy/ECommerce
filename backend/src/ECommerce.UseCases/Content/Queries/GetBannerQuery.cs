using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record GetBannerQuery(Guid BannerId) : IRequest<Result<BannerResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentBannerRead;
}

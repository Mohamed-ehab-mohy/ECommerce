using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record AdminListBannersQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedBannersResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentBannerRead;
}

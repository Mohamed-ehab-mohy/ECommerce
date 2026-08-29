using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record AdminListCmsLayoutsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedCmsLayoutsResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentLayoutRead;
}

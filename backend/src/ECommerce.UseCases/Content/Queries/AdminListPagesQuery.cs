using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record AdminListPagesQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedPagesResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentPageRead;
}

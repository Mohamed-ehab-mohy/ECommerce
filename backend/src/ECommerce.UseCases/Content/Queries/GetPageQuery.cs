using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record GetPageQuery(Guid PageId) : IRequest<Result<PageResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentPageRead;
}

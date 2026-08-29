using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Responses;

namespace ECommerce.UseCases.Content.Queries;

public sealed record GetCmsLayoutQuery(Guid LayoutId) : IRequest<Result<CmsLayoutResponse>>, IRequirePermission
{
    public string Permission => Permissions.ContentLayoutRead;
}

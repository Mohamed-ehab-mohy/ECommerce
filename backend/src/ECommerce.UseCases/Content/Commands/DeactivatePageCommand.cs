using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Content.Commands;

public sealed record DeactivatePageCommand(Guid PageId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.ContentPageDelete;
}

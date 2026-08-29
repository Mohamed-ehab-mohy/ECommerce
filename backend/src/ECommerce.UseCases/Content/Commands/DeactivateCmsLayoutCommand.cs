using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Content.Commands;

public sealed record DeactivateCmsLayoutCommand(Guid LayoutId) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.ContentLayoutDelete;
}

using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    IReadOnlyList<string>? Permissions = null) : IRequest<Result<Guid>>, IRequirePermission
{
    public string Permission => ECommerce.Shared.Authorization.Permissions.RolesWrite;
}

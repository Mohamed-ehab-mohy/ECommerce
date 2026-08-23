using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record AssignRolePermissionsCommand(
    Guid RoleId,
    IReadOnlyList<string> Permissions) : IRequest<Result>, IRequirePermission
{
    public string Permission => ECommerce.Shared.Authorization.Permissions.RolesPermissionsWrite;
}

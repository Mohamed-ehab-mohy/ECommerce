using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record ListRolesQuery : IRequest<Result<IReadOnlyList<RoleResponse>>>, IRequirePermission
{
    public string Permission => Permissions.RolesRead;
}

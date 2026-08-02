using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Identity.Queries;
using ECommerce.UseCases.Identity.Responses;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ListRolesQueryHandler(IRoleRepository roles)
    : IRequestHandler<ListRolesQuery, Result<IReadOnlyList<RoleResponse>>>
{
    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(
        ListRolesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await roles.ListAsync(cancellationToken);

        return Result<IReadOnlyList<RoleResponse>>.Success(items
            .Select(role => new RoleResponse(
                role.Id,
                role.Name,
                role.Description,
                role.Permissions.Select(permission => permission.PermissionCode).ToList()))
            .ToList());
    }
}

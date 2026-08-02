using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class AssignRolePermissionsCommandHandler(
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<AssignRolePermissionsCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<AssignRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(AssignRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var role = await roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.RoleNotFound);
        }

        var permissionCodes = request.Permissions
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        if (permissionCodes.Any(code => !Permissions.All.Contains(code, StringComparer.Ordinal)))
        {
            return Result.Failure(RoleErrors.PermissionNotRegistered);
        }

        var before = role.Permissions.Select(permission => permission.PermissionCode).ToList();
        role.AssignPermissions(permissionCodes, timeProvider.GetUtcNow().UtcDateTime);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.RolePermissionsChanged,
            "Role",
            role.Id.ToString(),
            Before: new { permissionCodes = before },
            After: new { permissionCodes }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

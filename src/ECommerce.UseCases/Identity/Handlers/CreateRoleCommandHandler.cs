using ECommerce.Domain.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateRoleCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        var name = request.Name.Trim();
        if (await roles.GetByNameAsync(name, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(RoleErrors.NameAlreadyExists);
        }

        var permissionCodes = request.Permissions
            ?.Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList() ?? [];

        var role = Role.Create(name, request.Description, timeProvider.GetUtcNow().UtcDateTime);
        role.AssignPermissions(permissionCodes, timeProvider.GetUtcNow().UtcDateTime);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.RoleCreated,
            "Role",
            role.Id.ToString(),
            After: new { role.Name, role.Description, permissionCodes }), cancellationToken);

        roles.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.Id);
    }
}

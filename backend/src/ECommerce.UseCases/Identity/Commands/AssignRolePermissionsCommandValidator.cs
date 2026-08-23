
namespace ECommerce.UseCases.Identity.Commands;

public sealed class AssignRolePermissionsCommandValidator : AbstractValidator<AssignRolePermissionsCommand>
{
    public AssignRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty();

        RuleFor(x => x.Permissions)
            .NotNull()
            .Must(permissions => permissions.All(IsKnown))
            .WithMessage("Unknown permission code.");
    }

    private static bool IsKnown(string permission) =>
        ECommerce.Shared.Authorization.Permissions.All.Contains(permission, StringComparer.Ordinal);
}

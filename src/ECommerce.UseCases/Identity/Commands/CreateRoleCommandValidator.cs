using FluentValidation;

namespace ECommerce.UseCases.Identity.Commands;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(60);

        RuleFor(x => x.Description)
            .MaximumLength(300)
            .When(x => x.Description is not null);

        RuleFor(x => x.Permissions)
            .Must(permissions => permissions is null || permissions.All(IsKnown))
            .WithMessage("Unknown permission code.")
            .When(x => x.Permissions is not null);
    }

    private static bool IsKnown(string permission) =>
        ECommerce.Shared.Authorization.Permissions.All.Contains(permission, StringComparer.Ordinal);
}

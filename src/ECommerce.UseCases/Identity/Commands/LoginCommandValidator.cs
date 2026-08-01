using FluentValidation;

namespace ECommerce.UseCases.Identity.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .MaximumLength(64);
    }
}

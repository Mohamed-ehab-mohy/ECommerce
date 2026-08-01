using FluentValidation;

namespace ECommerce.UseCases.Identity.Commands;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Locale)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);
    }
}

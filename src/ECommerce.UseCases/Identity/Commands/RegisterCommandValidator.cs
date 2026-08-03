using ECommerce.UseCases.Pricing;
using FluentValidation;

namespace ECommerce.UseCases.Identity.Commands;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(ICurrencyCatalog currencies, ILocaleCatalog locales)
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
            .Must(locales.IsSupported)
            .WithMessage("'{PropertyValue}' is not a supported locale.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(currencies.IsSupported)
            .WithMessage("'{PropertyValue}' is not a supported currency.");
    }
}


namespace ECommerce.UseCases.Identity.Commands;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(100);
        });

        When(x => x.Phone is not null, () =>
        {
            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .Matches("^\\+[1-9]\\d{7,14}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        });

        When(x => x.Locale is not null, () =>
        {
            RuleFor(x => x.Locale)
                .NotEmpty()
                .MaximumLength(10);
        });

        When(x => x.Currency is not null, () =>
        {
            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3);
        });

        RuleFor(x => x)
            .Must(x => x.DisplayName is not null || x.Phone is not null || x.Locale is not null || x.Currency is not null)
            .WithMessage("At least one profile field must be provided.");
    }
}

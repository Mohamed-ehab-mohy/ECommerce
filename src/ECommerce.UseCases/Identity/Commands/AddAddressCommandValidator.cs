
namespace ECommerce.UseCases.Identity.Commands;

public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();

        RuleFor(x => x.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Region)
            .MaximumLength(100)
            .When(x => x.Region is not null);

        RuleFor(x => x.Country)
            .NotEmpty()
            .Length(2)
            .Matches("^[a-zA-Z]{2}$");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .When(x => x.PostalCode is not null);

        RuleFor(x => x.Label)
            .MaximumLength(50)
            .When(x => x.Label is not null);
    }
}

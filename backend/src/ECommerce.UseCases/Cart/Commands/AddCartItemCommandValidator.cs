using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Cart.Commands;

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator(ICurrencyCatalog currencies)
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Currency)
            .Must(currencies.IsSupported)
            .WithMessage("'{PropertyValue}' is not a supported currency.");
        RuleFor(command => command.Quantity).InclusiveBetween(1, 99);
    }
}

using ECommerce.UseCases.Pricing;
using FluentValidation;

namespace ECommerce.UseCases.Wishlist.Commands;

public sealed class MoveWishlistItemToCartCommandValidator : AbstractValidator<MoveWishlistItemToCartCommand>
{
    public MoveWishlistItemToCartCommandValidator(ICurrencyCatalog currencies)
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Currency)
            .Must(currencies.IsSupported)
            .WithMessage("'{PropertyValue}' is not a supported currency.");
    }
}

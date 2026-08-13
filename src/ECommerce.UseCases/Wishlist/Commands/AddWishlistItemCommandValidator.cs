using FluentValidation;

namespace ECommerce.UseCases.Wishlist.Commands;

public sealed class AddWishlistItemCommandValidator : AbstractValidator<AddWishlistItemCommand>
{
    public AddWishlistItemCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.ProductId).NotEmpty();
    }
}

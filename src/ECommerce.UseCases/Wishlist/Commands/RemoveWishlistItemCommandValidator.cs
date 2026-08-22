
namespace ECommerce.UseCases.Wishlist.Commands;

public sealed class RemoveWishlistItemCommandValidator : AbstractValidator<RemoveWishlistItemCommand>
{
    public RemoveWishlistItemCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.ProductId).NotEmpty();
    }
}

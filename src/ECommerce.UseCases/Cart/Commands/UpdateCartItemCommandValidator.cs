using ECommerce.UseCases.Cart.Commands;

namespace ECommerce.UseCases.Cart.Commands;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Quantity).InclusiveBetween(0, 99);
    }
}

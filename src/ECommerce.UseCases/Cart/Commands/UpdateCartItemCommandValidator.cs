using ECommerce.UseCases.Cart.Commands;
using FluentValidation;

namespace ECommerce.UseCases.Cart.Commands;

public sealed class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Quantity).InclusiveBetween(0, 99);
    }
}

using ECommerce.UseCases.Cart.Commands;
using FluentValidation;

namespace ECommerce.UseCases.Cart.Commands;

public sealed class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
    }
}

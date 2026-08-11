using FluentValidation;

namespace ECommerce.UseCases.Cart.Commands;

public sealed class RemoveCartCouponCommandValidator : AbstractValidator<RemoveCartCouponCommand>
{
    public RemoveCartCouponCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
    }
}

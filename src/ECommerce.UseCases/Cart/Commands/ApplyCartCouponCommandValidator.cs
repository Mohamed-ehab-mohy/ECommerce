
namespace ECommerce.UseCases.Cart.Commands;

public sealed class ApplyCartCouponCommandValidator : AbstractValidator<ApplyCartCouponCommand>
{
    public ApplyCartCouponCommandValidator()
    {
        RuleFor(command => command.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Code).NotEmpty().MaximumLength(64);
    }
}

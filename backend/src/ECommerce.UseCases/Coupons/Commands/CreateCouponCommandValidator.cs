
namespace ECommerce.UseCases.Coupons.Commands;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(64);
        RuleFor(command => command.PromotionId).NotEmpty();
        RuleFor(command => command.TotalUses).GreaterThan(0);
        RuleFor(command => command.PerCustomerLimit).GreaterThan(0).When(command => command.PerCustomerLimit is not null);
        RuleFor(command => command.StartsAt)
            .LessThanOrEqualTo(command => command.EndsAt)
            .When(command => command.StartsAt is not null && command.EndsAt is not null);
    }
}

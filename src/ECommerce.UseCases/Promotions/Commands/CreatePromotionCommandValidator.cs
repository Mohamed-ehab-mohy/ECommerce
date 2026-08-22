
namespace ECommerce.UseCases.Promotions.Commands;

public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Actions).NotEmpty();
        RuleForEach(command => command.Actions).ChildRules(action =>
        {
            action.RuleFor(input => input.Value).GreaterThan(0m);
            action.RuleFor(input => input.Basis).IsInEnum();
            action.RuleFor(input => input.Type).IsInEnum();
        });
        RuleFor(command => command.StartsAt)
            .LessThanOrEqualTo(command => command.EndsAt)
            .When(command => command.StartsAt is not null && command.EndsAt is not null);
    }
}

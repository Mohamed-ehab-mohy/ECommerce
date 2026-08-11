using FluentValidation;

namespace ECommerce.UseCases.Promotions.Commands;

public sealed class UpdatePromotionCommandValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Actions).NotEmpty();
    }
}

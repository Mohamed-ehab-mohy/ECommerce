using FluentValidation;

namespace ECommerce.UseCases.Wallets.Commands;

public sealed class ConvertPointsCommandValidator : AbstractValidator<ConvertPointsCommand>
{
    public ConvertPointsCommandValidator()
    {
        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points to convert must be greater than zero.")
            .LessThanOrEqualTo(10000).WithMessage("Cannot convert more than 10,000 points at a time.");
    }
}


namespace ECommerce.UseCases.Reviews.Commands;

public sealed class RemoveReviewCommandValidator : AbstractValidator<RemoveReviewCommand>
{
    public RemoveReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId).NotEmpty();
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

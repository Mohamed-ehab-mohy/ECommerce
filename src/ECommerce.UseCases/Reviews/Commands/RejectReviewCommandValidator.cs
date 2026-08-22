
namespace ECommerce.UseCases.Reviews.Commands;

public sealed class RejectReviewCommandValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId).NotEmpty();
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

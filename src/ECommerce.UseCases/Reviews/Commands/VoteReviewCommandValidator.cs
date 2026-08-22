
namespace ECommerce.UseCases.Reviews.Commands;

public sealed class VoteReviewCommandValidator : AbstractValidator<VoteReviewCommand>
{
    public VoteReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
    }
}

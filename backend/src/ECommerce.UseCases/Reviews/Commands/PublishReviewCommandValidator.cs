
namespace ECommerce.UseCases.Reviews.Commands;

public sealed class PublishReviewCommandValidator : AbstractValidator<PublishReviewCommand>
{
    public PublishReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId).NotEmpty();
    }
}


namespace ECommerce.UseCases.Reviews.Commands;

public sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public const int MaxCommentLength = 2000;

    public SubmitReviewCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
        RuleFor(command => command.Comment)
            .NotEmpty()
            .MaximumLength(MaxCommentLength);
    }
}

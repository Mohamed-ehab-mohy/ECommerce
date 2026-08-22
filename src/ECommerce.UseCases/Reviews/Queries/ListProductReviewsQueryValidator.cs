
namespace ECommerce.UseCases.Reviews.Queries;

public sealed class ListProductReviewsQueryValidator : AbstractValidator<ListProductReviewsQuery>
{
    public ListProductReviewsQueryValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(50);
    }
}

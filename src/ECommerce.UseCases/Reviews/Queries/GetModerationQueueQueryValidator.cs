
namespace ECommerce.UseCases.Reviews.Queries;

public sealed class GetModerationQueueQueryValidator : AbstractValidator<GetModerationQueueQuery>
{
    public GetModerationQueueQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(50);
    }
}

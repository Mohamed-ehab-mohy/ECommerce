namespace ECommerce.UseCases.Content.Queries;

public sealed class GetPageQueryValidator : AbstractValidator<GetPageQuery>
{
    public GetPageQueryValidator()
    {
        RuleFor(x => x.PageId).NotEmpty();
    }
}

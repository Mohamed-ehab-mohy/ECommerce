namespace ECommerce.UseCases.Content.Queries;

public sealed class GetPageBySlugQueryValidator : AbstractValidator<GetPageBySlugQuery>
{
    public GetPageBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
    }
}

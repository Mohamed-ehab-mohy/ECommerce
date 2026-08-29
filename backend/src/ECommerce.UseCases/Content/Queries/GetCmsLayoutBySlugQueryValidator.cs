namespace ECommerce.UseCases.Content.Queries;

public sealed class GetCmsLayoutBySlugQueryValidator : AbstractValidator<GetCmsLayoutBySlugQuery>
{
    public GetCmsLayoutBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
    }
}

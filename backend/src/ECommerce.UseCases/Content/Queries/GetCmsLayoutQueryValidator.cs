namespace ECommerce.UseCases.Content.Queries;

public sealed class GetCmsLayoutQueryValidator : AbstractValidator<GetCmsLayoutQuery>
{
    public GetCmsLayoutQueryValidator()
    {
        RuleFor(x => x.LayoutId).NotEmpty();
    }
}

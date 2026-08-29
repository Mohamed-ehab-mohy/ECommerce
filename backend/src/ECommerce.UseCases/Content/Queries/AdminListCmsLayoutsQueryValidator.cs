namespace ECommerce.UseCases.Content.Queries;

public sealed class AdminListCmsLayoutsQueryValidator : AbstractValidator<AdminListCmsLayoutsQuery>
{
    public AdminListCmsLayoutsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
    }
}

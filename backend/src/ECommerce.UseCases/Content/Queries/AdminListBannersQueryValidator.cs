namespace ECommerce.UseCases.Content.Queries;

public sealed class AdminListBannersQueryValidator : AbstractValidator<AdminListBannersQuery>
{
    public AdminListBannersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
    }
}

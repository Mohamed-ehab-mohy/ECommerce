
namespace ECommerce.UseCases.Catalog.Queries;

public sealed class ListBrandsQueryValidator : AbstractValidator<ListBrandsQuery>
{
    public ListBrandsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
    }
}

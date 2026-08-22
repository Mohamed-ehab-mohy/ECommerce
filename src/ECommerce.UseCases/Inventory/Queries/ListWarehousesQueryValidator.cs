
namespace ECommerce.UseCases.Inventory.Queries;

public sealed class ListWarehousesQueryValidator : AbstractValidator<ListWarehousesQuery>
{
    public ListWarehousesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
    }
}

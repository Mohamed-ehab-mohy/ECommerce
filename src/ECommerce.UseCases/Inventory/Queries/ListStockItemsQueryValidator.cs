
namespace ECommerce.UseCases.Inventory.Queries;

public sealed class ListStockItemsQueryValidator : AbstractValidator<ListStockItemsQuery>
{
    public ListStockItemsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}

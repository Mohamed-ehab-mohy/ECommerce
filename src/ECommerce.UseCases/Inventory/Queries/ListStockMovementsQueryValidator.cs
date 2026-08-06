using FluentValidation;

namespace ECommerce.UseCases.Inventory.Queries;

public sealed class ListStockMovementsQueryValidator : AbstractValidator<ListStockMovementsQuery>
{
    public ListStockMovementsQueryValidator()
    {
        RuleFor(x => x.StockItemId)
            .NotEmpty();

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
    }
}

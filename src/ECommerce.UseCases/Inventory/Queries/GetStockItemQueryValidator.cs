
namespace ECommerce.UseCases.Inventory.Queries;

public sealed class GetStockItemQueryValidator : AbstractValidator<GetStockItemQuery>
{
    public GetStockItemQueryValidator()
    {
        RuleFor(x => x.StockItemId)
            .NotEmpty();
    }
}

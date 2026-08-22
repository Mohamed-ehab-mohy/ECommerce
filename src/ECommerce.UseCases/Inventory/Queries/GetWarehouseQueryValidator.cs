
namespace ECommerce.UseCases.Inventory.Queries;

public sealed class GetWarehouseQueryValidator : AbstractValidator<GetWarehouseQuery>
{
    public GetWarehouseQueryValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}


namespace ECommerce.UseCases.Inventory.Commands;

public sealed class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.FromWarehouseId)
            .NotEmpty();

        RuleFor(x => x.ToWarehouseId)
            .NotEmpty()
            .NotEqual(x => x.FromWarehouseId);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null);
    }
}

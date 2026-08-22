using ECommerce.Domain.Inventory;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed class PostStockMovementCommandValidator : AbstractValidator<PostStockMovementCommand>
{
    public PostStockMovementCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.WarehouseId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .NotEmpty()
            .IsEnumName(typeof(StockMovementType), caseSensitive: false);

        RuleFor(x => x.Quantity)
            .NotEqual(0)
            .GreaterThan(0)
            .When(x => !IsAdjustment(x.Type));

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Reference)
            .MaximumLength(100)
            .When(x => x.Reference is not null);

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null);
    }

    private static bool IsAdjustment(string type) =>
        string.Equals(type, nameof(StockMovementType.Adjustment), StringComparison.OrdinalIgnoreCase);
}

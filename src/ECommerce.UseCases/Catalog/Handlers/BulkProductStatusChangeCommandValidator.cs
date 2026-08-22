using ECommerce.UseCases.Catalog.Commands;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class BulkProductStatusChangeCommandValidator : AbstractValidator<BulkProductStatusChangeCommand>
{
    public const int MaxBatchSize = 500;

    public BulkProductStatusChangeCommandValidator()
    {
        RuleFor(command => command.Items)
            .NotNull()
            .NotEmpty()
            .WithMessage("The batch must contain at least one item.");

        RuleFor(command => command.Items)
            .Must(items => items is not null && items.Count <= MaxBatchSize)
            .WithMessage($"The batch cannot exceed {MaxBatchSize} items.");

        RuleFor(command => command.Items)
            .Must(items => items is null || items.All(item => item.ProductId != Guid.Empty))
            .WithMessage("Every item must have a product id.");
    }
}

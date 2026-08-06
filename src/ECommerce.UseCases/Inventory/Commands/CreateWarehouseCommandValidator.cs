using ECommerce.Domain.Inventory;
using FluentValidation;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Status)
            .IsEnumName(typeof(WarehouseStatus), caseSensitive: false)
            .When(x => x.Status is not null);
    }
}

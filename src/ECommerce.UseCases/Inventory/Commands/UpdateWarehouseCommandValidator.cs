using ECommerce.Domain.Inventory;

namespace ECommerce.UseCases.Inventory.Commands;

public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(160);
        });

        When(x => x.Address is not null, () =>
        {
            RuleFor(x => x.Address)
                .NotEmpty()
                .MaximumLength(500);
        });

        When(x => x.Timezone is not null, () =>
        {
            RuleFor(x => x.Timezone)
                .NotEmpty()
                .MaximumLength(64);
        });

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status)
                .IsEnumName(typeof(WarehouseStatus), caseSensitive: false);
        });

        RuleFor(x => x)
            .Must(x => x.Name is not null ||
                       x.Address is not null ||
                       x.Timezone is not null ||
                       x.Status is not null)
            .WithMessage("At least one warehouse field must be provided.");
    }
}

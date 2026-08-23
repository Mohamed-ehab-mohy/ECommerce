
namespace ECommerce.UseCases.Catalog.Commands;

public sealed class StartProductImportCommandValidator : AbstractValidator<StartProductImportCommand>
{
    public const int MaxBatchSize = 5_000;

    public StartProductImportCommandValidator()
    {
        RuleFor(command => command.Rows)
            .NotNull()
            .NotEmpty()
            .WithMessage("The import batch must contain at least one row.");

        RuleFor(command => command.Rows)
            .Must(rows => rows is not null && rows.Count <= MaxBatchSize)
            .WithMessage($"The import batch cannot exceed {MaxBatchSize} rows.");
    }
}

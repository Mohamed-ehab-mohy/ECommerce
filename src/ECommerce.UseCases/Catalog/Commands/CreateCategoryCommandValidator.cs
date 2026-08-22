
namespace ECommerce.UseCases.Catalog.Commands;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(160)
            .Matches("^[a-z0-9-]+$");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

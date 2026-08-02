using FluentValidation;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(120);
        });

        When(x => x.Slug is not null, () =>
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(160)
                .Matches("^[a-z0-9-]+$");
        });

        When(x => x.SortOrder is not null, () =>
        {
            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0);
        });

        RuleFor(x => x)
            .Must(x => x.Name is not null ||
                       x.Slug is not null ||
                       x.ParentId is not null ||
                       x.SortOrder is not null)
            .WithMessage("At least one category field must be provided.");
    }
}


namespace ECommerce.UseCases.Catalog.Commands;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.BrandId).NotEmpty();

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(160);
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000);
        });

        When(x => x.Website is not null, () =>
        {
            RuleFor(x => x.Website)
                .MaximumLength(255)
                .Matches("^https?://");
        });

        RuleFor(x => x)
            .Must(x => x.Name is not null ||
                       x.Description is not null ||
                       x.Website is not null)
            .WithMessage("At least one brand field must be provided.");
    }
}

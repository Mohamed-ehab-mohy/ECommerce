
namespace ECommerce.UseCases.Catalog.Commands;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Website)
            .MaximumLength(255)
            .Matches("^https?://")
            .When(x => x.Website is not null);
    }
}

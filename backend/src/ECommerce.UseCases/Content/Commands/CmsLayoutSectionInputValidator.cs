namespace ECommerce.UseCases.Content.Commands;

public sealed class CmsLayoutSectionInputValidator : AbstractValidator<CmsLayoutSectionInput>
{
    public CmsLayoutSectionInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}

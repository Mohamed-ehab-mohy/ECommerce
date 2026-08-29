namespace ECommerce.UseCases.Content.Commands;

public sealed class UpdateCmsLayoutCommandValidator : AbstractValidator<UpdateCmsLayoutCommand>
{
    public UpdateCmsLayoutCommandValidator()
    {
        RuleFor(x => x.LayoutId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");

        RuleForEach(x => x.Sections)
            .SetValidator(new CmsLayoutSectionInputValidator());
    }
}

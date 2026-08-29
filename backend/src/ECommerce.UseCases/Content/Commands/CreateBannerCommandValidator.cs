namespace ECommerce.UseCases.Content.Commands;

public sealed class CreateBannerCommandValidator : AbstractValidator<CreateBannerCommand>
{
    public CreateBannerCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ImageUrl)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.TargetUrl)
            .MaximumLength(2048)
            .When(x => x.TargetUrl is not null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}

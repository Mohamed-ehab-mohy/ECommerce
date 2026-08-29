namespace ECommerce.UseCases.Content.Commands;

public sealed class UpdateBannerCommandValidator : AbstractValidator<UpdateBannerCommand>
{
    public UpdateBannerCommandValidator()
    {
        RuleFor(x => x.BannerId)
            .NotEmpty();

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

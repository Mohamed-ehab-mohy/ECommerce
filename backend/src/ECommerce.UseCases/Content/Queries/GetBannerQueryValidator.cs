namespace ECommerce.UseCases.Content.Queries;

public sealed class GetBannerQueryValidator : AbstractValidator<GetBannerQuery>
{
    public GetBannerQueryValidator()
    {
        RuleFor(x => x.BannerId).NotEmpty();
    }
}

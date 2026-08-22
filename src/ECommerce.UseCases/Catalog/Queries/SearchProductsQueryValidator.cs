using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator(ICurrencyCatalog currencies, ILocaleCatalog locales)
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
        RuleFor(x => x.Q).MaximumLength(200);
        RuleFor(x => x.PriceGte).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceLte).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RatingGte).InclusiveBetween(0, 5);
        RuleFor(x => x)
            .Must(x => x.PriceLte is null || x.PriceGte is null || x.PriceGte <= x.PriceLte)
            .WithMessage("'PriceGte' must not exceed 'PriceLte'.");
        RuleFor(x => x.Locale)
            .Must(locale => locales.IsSupported(locale))
            .WithMessage("'{PropertyValue}' is not a supported locale.")
            .When(x => x.Locale is not null);
        RuleFor(x => x.Currency)
            .Must(currency => currencies.IsSupported(currency))
            .WithMessage("'{PropertyValue}' is not a supported currency.")
            .When(x => x.Currency is not null);
    }
}

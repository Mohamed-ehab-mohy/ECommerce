using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator(ICurrencyCatalog currencies, ILocaleCatalog locales)
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(100);
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

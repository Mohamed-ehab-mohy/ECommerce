using ECommerce.UseCases.Pricing;
using FluentValidation;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed class GetProductQueryValidator : AbstractValidator<GetProductQuery>
{
    public GetProductQueryValidator(ICurrencyCatalog currencies, ILocaleCatalog locales)
    {
        RuleFor(x => x.ProductId).NotEmpty();
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

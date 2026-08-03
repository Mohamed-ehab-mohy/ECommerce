using ECommerce.Domain.Catalog;
using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Catalog.Responses;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Slug,
    string Name,
    string? Description,
    string Currency,
    decimal ListAmount,
    decimal? OfferAmount,
    ProductStatus Status,
    bool IsFeatured,
    Guid? CategoryId,
    Guid? BrandId);

public static class ProductResponseFactory
{
    public static ProductResponse From(
        Product product,
        ILocaleCatalog locales,
        ICurrencyCatalog currencies,
        string? locale = null,
        string? currency = null)
    {
        var translation = SelectTranslation(product, locales, locale);
        var (listAmount, offerAmount, resolvedCurrency) = SelectPrice(product, currencies, currency);

        return new ProductResponse(
            product.Id,
            product.Sku,
            product.Slug,
            translation?.Name ?? string.Empty,
            translation?.Description,
            resolvedCurrency,
            listAmount,
            offerAmount,
            product.Status,
            product.IsFeatured,
            product.CategoryId,
            product.BrandId);
    }

    private static ProductTranslation? SelectTranslation(
        Product product,
        ILocaleCatalog locales,
        string? locale)
    {
        var requested = string.IsNullOrWhiteSpace(locale) ? null : locale.Trim().ToLowerInvariant();

        if (requested is not null && locales.IsSupported(requested))
        {
            var match = product.Translations.FirstOrDefault(item => item.Locale == requested);
            if (match is not null)
            {
                return match;
            }

            var fallback = product.Translations.FirstOrDefault(item => item.Locale == locales.DefaultLocale);
            if (fallback is not null)
            {
                return fallback;
            }
        }

        return product.Translations.FirstOrDefault();
    }

    private static (decimal ListAmount, decimal? OfferAmount, string Currency) SelectPrice(
        Product product,
        ICurrencyCatalog currencies,
        string? currency)
    {
        var requested = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();

        if (requested is not null && currencies.IsSupported(requested))
        {
            var match = product.Prices.FirstOrDefault(item => item.Currency == requested);
            if (match is not null)
            {
                return ToDisplay(match.ListAmount, match.OfferAmount, match.Currency);
            }
        }

        var source = product.Prices.FirstOrDefault();
        if (source is null)
        {
            return (0m, null, string.Empty);
        }

        if (requested is null ||
            requested == source.Currency ||
            !currencies.IsSupported(source.Currency))
        {
            return ToDisplay(source.ListAmount, source.OfferAmount, source.Currency);
        }

        var rate = currencies.GetRate(source.Currency, requested);
        var converted = Money.From(source.ListAmount, source.Currency).ConvertTo(requested, rate);
        decimal? offer = source.OfferAmount is null
            ? null
            : Money.From(source.OfferAmount.Value, source.Currency).ConvertTo(requested, rate).DisplayAmount;

        return (converted.DisplayAmount, offer, converted.Currency);
    }

    private static (decimal ListAmount, decimal? OfferAmount, string Currency) ToDisplay(
        decimal listAmount,
        decimal? offerAmount,
        string currency)
    {
        return (
            Money.From(listAmount, currency).DisplayAmount,
            offerAmount is null ? null : Money.From(offerAmount.Value, currency).DisplayAmount,
            currency);
    }
}

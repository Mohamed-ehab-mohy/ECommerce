namespace ECommerce.UseCases.Pricing;

public interface ILocaleCatalog
{
    IReadOnlyList<string> SupportedLocales { get; }

    string DefaultLocale { get; }

    bool IsSupported(string? locale);
}

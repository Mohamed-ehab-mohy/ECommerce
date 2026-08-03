namespace ECommerce.UseCases.Pricing;

public sealed class DefaultLocaleCatalog : ILocaleCatalog
{
    private static readonly HashSet<string> Codes = new(
        ["en", "ar", "fr", "de", "es", "it", "pt", "tr", "ru", "zh"],
        StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> SupportedLocales { get; } = ["en", "ar", "fr", "de", "es", "it", "pt", "tr", "ru", "zh"];

    public string DefaultLocale { get; } = "en";

    public bool IsSupported(string? locale) =>
        !string.IsNullOrWhiteSpace(locale) && Codes.Contains(locale.Trim());
}

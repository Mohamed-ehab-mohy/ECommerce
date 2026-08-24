using ECommerce.Domain.Common;

namespace ECommerce.Domain.Tenants;

public sealed class TenantSettings : BaseEntity<Guid>
{
    public string DefaultCurrency { get; private set; } = "USD";
    public string DefaultLocale { get; private set; } = "en";
    public string? ThemeName { get; private set; }
    public string? LogoUrl { get; private set; }

    private TenantSettings() // For EF Core
    {
    }

    public TenantSettings(Guid tenantId)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
    }

    public void UpdatePreferences(string currency, string locale, string? theme, string? logoUrl)
    {
        DefaultCurrency = currency;
        DefaultLocale = locale;
        ThemeName = theme;
        LogoUrl = logoUrl;
    }
}

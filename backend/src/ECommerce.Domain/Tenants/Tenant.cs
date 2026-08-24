using ECommerce.Domain.Common;

namespace ECommerce.Domain.Tenants;

public enum TenantStatus
{
    Active = 0,
    Suspended = 1,
    PendingSetup = 2
}

public sealed class Tenant : BaseEntity<Guid>
{
    public string Name { get; private set; }
    public string Subdomain { get; private set; }
    public string? CustomDomain { get; private set; }
    public TenantStatus Status { get; private set; }
    public TenantSettings Settings { get; private set; } = null!;

    private Tenant() // For EF Core
    {
        Name = null!;
        Subdomain = null!;
    }

    public Tenant(string name, string subdomain)
    {
        Id = Guid.NewGuid();
        Name = name;
        Subdomain = subdomain.ToLowerInvariant();
        Status = TenantStatus.PendingSetup;
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
    }

    public void SetCustomDomain(string domain)
    {
        CustomDomain = domain.ToLowerInvariant();
    }

    public void SetSettings(TenantSettings settings)
    {
        Settings = settings;
    }
}

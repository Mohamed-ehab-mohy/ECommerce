using ECommerce.Domain.Common;
using System;

namespace ECommerce.Domain.Tenants;

public sealed class SubscriptionPlan : BaseEntity<Guid>
{
    public string Name { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public int MaxProducts { get; private set; }
    public int MaxUsers { get; private set; }
    public bool SupportsCustomDomain { get; private set; }
    public bool AdvancedAnalytics { get; private set; }
    public bool IsActive { get; private set; }

    private SubscriptionPlan()
    {
        Name = null!;
    }

    public SubscriptionPlan(string name, decimal monthlyPrice, int maxProducts, int maxUsers, bool supportsCustomDomain, bool advancedAnalytics)
    {
        Id = Guid.NewGuid();
        Name = name;
        MonthlyPrice = monthlyPrice;
        MaxProducts = maxProducts;
        MaxUsers = maxUsers;
        SupportsCustomDomain = supportsCustomDomain;
        AdvancedAnalytics = advancedAnalytics;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

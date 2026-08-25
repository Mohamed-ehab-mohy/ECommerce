using ECommerce.Domain.Common;
using System;

namespace ECommerce.Domain.Tenants;

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    PastDue = 2,
    Canceled = 3
}

public sealed class TenantSubscription : BaseEntity<Guid>
{
    public Guid PlanId { get; private set; }
    public SubscriptionPlan Plan { get; private set; } = null!;

    public SubscriptionStatus Status { get; private set; }
    public DateTime? CurrentPeriodEnd { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public string? StripeCustomerId { get; private set; }

    private TenantSubscription() { } // For EF Core

    public TenantSubscription(Guid tenantId, Guid planId, DateTime? trialEnd = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        PlanId = planId;
        Status = SubscriptionStatus.Trial;
        CurrentPeriodEnd = trialEnd ?? DateTime.UtcNow.AddDays(14);
    }

    public void MarkAsActive(string stripeCustomerId, string stripeSubId, DateTime periodEnd)
    {
        Status = SubscriptionStatus.Active;
        StripeCustomerId = stripeCustomerId;
        StripeSubscriptionId = stripeSubId;
        CurrentPeriodEnd = periodEnd;
    }

    public void MarkAsPastDue()
    {
        Status = SubscriptionStatus.PastDue;
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Canceled;
    }

    public bool IsActiveOrTrial()
    {
        return Status == SubscriptionStatus.Active ||
               (Status == SubscriptionStatus.Trial && CurrentPeriodEnd >= DateTime.UtcNow);
    }

    public void ChangePlan(Guid planId)
    {
        PlanId = planId;
    }
}

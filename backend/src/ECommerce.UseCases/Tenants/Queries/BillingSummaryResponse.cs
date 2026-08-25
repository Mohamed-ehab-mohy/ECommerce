using System;

namespace ECommerce.UseCases.Tenants.Queries;

public sealed record BillingSummaryResponse(
    string PlanName,
    decimal MonthlyPrice,
    string Status,
    DateTime? CurrentPeriodEnd,
    int CurrentProducts,
    int MaxProducts,
    bool SupportsCustomDomain,
    bool AdvancedAnalytics);
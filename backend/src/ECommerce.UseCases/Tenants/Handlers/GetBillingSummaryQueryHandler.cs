using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Tenants.Ports;
using ECommerce.UseCases.Tenants.Queries;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class GetBillingSummaryQueryHandler(
    ITenantService tenantService,
    ITenantRepository tenantRepository,
    IProductRepository productRepository)
    : IRequestHandler<GetBillingSummaryQuery, Result<BillingSummaryResponse>>
{
    public async Task<Result<BillingSummaryResponse>> Handle(GetBillingSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Result<BillingSummaryResponse>.Failure(new Error("Tenant.Unauthorized", "No active tenant context."));

        var tenant = await tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null || tenant.Subscription == null)
            return Result<BillingSummaryResponse>.Failure(new Error("Tenant.NotFound", "Tenant or subscription not found."));

        var plan = await tenantRepository.GetSubscriptionPlanAsync(tenantId.Value, cancellationToken);
        if (plan == null)
            return Result<BillingSummaryResponse>.Failure(new Error("Subscription.PlanNotFound", "Subscription plan not found."));

        var currentProducts = await productRepository.CountAsync(cancellationToken);

        var response = new BillingSummaryResponse(
            plan.Name,
            plan.MonthlyPrice,
            tenant.Subscription.Status.ToString(),
            tenant.Subscription.CurrentPeriodEnd,
            currentProducts,
            plan.MaxProducts,
            plan.SupportsCustomDomain,
            plan.AdvancedAnalytics
        );

        return Result<BillingSummaryResponse>.Success(response);
    }
}
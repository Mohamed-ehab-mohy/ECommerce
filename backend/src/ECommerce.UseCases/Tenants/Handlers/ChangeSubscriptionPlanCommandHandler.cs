using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Tenants.Commands;
using ECommerce.UseCases.Tenants.Ports;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class ChangeSubscriptionPlanCommandHandler(
    ITenantService tenantService,
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeSubscriptionPlanCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ChangeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Result<Guid>.Failure(new Error("Tenant.Unauthorized", "No active tenant context."));

        var tenant = await tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null || tenant.Subscription == null)
            return Result<Guid>.Failure(new Error("Tenant.NotFound", "Tenant or subscription not found."));

        var plan = await tenantRepository.GetPlanByIdAsync(request.PlanId, cancellationToken);
        if (plan == null || !plan.IsActive)
            return Result<Guid>.Failure(new Error("Subscription.PlanNotFound", "The requested plan does not exist or is inactive."));

        // In a real SaaS, this is where we'd call Stripe/Payment Gateway to update the subscription
        // and handle prorations. For now, we simulate the database update directly.

        tenant.Subscription.ChangePlan(plan.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tenant.Id);
    }
}
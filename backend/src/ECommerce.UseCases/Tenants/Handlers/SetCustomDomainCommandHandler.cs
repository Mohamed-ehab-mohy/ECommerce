using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Tenants.Commands;
using ECommerce.UseCases.Tenants.Ports;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class SetCustomDomainCommandHandler(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork,
    ITenantService tenantService)
    : IRequestHandler<SetCustomDomainCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SetCustomDomainCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (!tenantId.HasValue)
            return Result<Guid>.Failure(new Error("Tenant.Unauthorized", "No active tenant found."));

        var tenant = await tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null)
            return Result<Guid>.Failure(new Error("Tenant.NotFound", "Tenant not found."));

        var plan = await tenantRepository.GetSubscriptionPlanAsync(tenantId.Value, cancellationToken);
        if (plan == null || !plan.SupportsCustomDomain)
        {
            return Result<Guid>.Failure(new Error("Subscription.FeatureNotAvailable", "Your current subscription plan does not support custom domains."));
        }

        var customDomain = request.CustomDomain?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(customDomain))
        {
            if (!await tenantRepository.IsCustomDomainUniqueAsync(customDomain, cancellationToken))
            {
                return Result<Guid>.Failure(new Error("Tenant.CustomDomainInUse", "The specified custom domain is already in use."));
            }
            tenant.SetCustomDomain(customDomain);
        }
        else
        {
            tenant.SetCustomDomain(null);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(tenant.Id);
    }
}

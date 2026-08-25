using ECommerce.Domain.Tenants;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Tenants.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Tenants;

internal sealed class TenantRepository(ECommerceDbContext dbContext) : ITenantRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await dbContext.Tenants.AddAsync(tenant, cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
    }

    public async Task<SubscriptionPlan?> GetSubscriptionPlanAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.TenantSubscriptions
            .Include(ts => ts.Plan)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId, cancellationToken);

        return subscription?.Plan;
    }

    public async Task<SubscriptionPlan?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SubscriptionPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
    }

    public async Task<bool> IsSubdomainUniqueAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        // Using IgnoreQueryFilters to ensure uniqueness across all tenants (even if global filter is somehow active)
        return !await dbContext.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Subdomain == subdomain, cancellationToken);
    }

    public async Task<bool> IsCustomDomainUniqueAsync(string customDomain, CancellationToken cancellationToken = default)
    {
        return !await dbContext.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.CustomDomain == customDomain, cancellationToken);
    }

    public async Task<Guid?> GetIdByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.CustomDomain == domain || domain.StartsWith(t.Subdomain + "."), cancellationToken);

        return tenant?.Id;
    }
}

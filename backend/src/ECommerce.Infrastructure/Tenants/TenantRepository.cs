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
}

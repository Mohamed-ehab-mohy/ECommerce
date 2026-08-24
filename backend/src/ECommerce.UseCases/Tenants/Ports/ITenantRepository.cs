using ECommerce.Domain.Tenants;

namespace ECommerce.UseCases.Tenants.Ports;

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task<bool> IsSubdomainUniqueAsync(string subdomain, CancellationToken cancellationToken = default);
    Task<bool> IsCustomDomainUniqueAsync(string customDomain, CancellationToken cancellationToken = default);
    Task<Guid?> GetIdByDomainAsync(string domain, CancellationToken cancellationToken = default);
}

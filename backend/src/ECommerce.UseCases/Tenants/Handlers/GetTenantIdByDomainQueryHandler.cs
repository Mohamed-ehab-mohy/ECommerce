using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Tenants.Ports;
using ECommerce.UseCases.Tenants.Queries;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class GetTenantIdByDomainQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantIdByDomainQuery, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GetTenantIdByDomainQuery request, CancellationToken cancellationToken)
    {
        var tenantId = await tenantRepository.GetIdByDomainAsync(request.Domain, cancellationToken);
        return !tenantId.HasValue
            ? Result<Guid>.Failure(new Error("Tenant.NotFound", "Tenant not found for the specified domain."))
            : Result<Guid>.Success(tenantId.Value);
    }
}

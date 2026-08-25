using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Platform.Queries;
using ECommerce.UseCases.Tenants.Ports;
using ECommerce.UseCases.Tenants.Responses;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.UseCases.Platform.Handlers;

internal sealed class ListAllTenantsQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<ListAllTenantsQuery, Result<IEnumerable<TenantResponse>>>
{
    public async Task<Result<IEnumerable<TenantResponse>>> Handle(ListAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetAllAsync(cancellationToken);
        
        var responses = tenants.Select(t => new TenantResponse(
            t.Id,
            t.Name,
            t.Subdomain,
            t.CustomDomain,
            t.Status.ToString()
        ));

        return Result<IEnumerable<TenantResponse>>.Success(responses);
    }
}

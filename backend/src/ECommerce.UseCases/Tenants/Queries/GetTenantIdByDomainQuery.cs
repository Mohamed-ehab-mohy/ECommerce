using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Tenants.Queries;

public sealed record GetTenantIdByDomainQuery(string Domain) : IRequest<Result<Guid>>;

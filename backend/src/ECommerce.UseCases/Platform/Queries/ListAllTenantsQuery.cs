using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Tenants.Responses;
using MediatR;
using System.Collections.Generic;

namespace ECommerce.UseCases.Platform.Queries;

public sealed record ListAllTenantsQuery() : IRequest<Result<IEnumerable<TenantResponse>>>;

using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Tenants.Responses;
using MediatR;

namespace ECommerce.UseCases.Tenants.Commands;

public sealed record CreateTenantCommand(
    string Name,
    string Subdomain,
    string? CustomDomain,
    string AdminEmail,
    string AdminPassword) : IRequest<Result<TenantResponse>>;

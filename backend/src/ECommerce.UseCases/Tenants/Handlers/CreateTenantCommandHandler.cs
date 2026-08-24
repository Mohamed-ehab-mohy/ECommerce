using ECommerce.Domain.Tenants;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Tenants.Commands;
using ECommerce.UseCases.Tenants.Ports;
using ECommerce.UseCases.Tenants.Responses;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTenantCommand, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var subdomain = request.Subdomain.ToLowerInvariant();
        if (!await tenantRepository.IsSubdomainUniqueAsync(subdomain, cancellationToken))
        {
            return Result<TenantResponse>.Failure(new Error("Tenant.SubdomainInUse", "The specified subdomain is already in use."));
        }

        if (!string.IsNullOrEmpty(request.CustomDomain))
        {
            var customDomain = request.CustomDomain.ToLowerInvariant();
            if (!await tenantRepository.IsCustomDomainUniqueAsync(customDomain, cancellationToken))
            {
                return Result<TenantResponse>.Failure(new Error("Tenant.CustomDomainInUse", "The specified custom domain is already in use."));
            }
        }

        var tenant = new Tenant(request.Name, request.Subdomain);
        if (!string.IsNullOrEmpty(request.CustomDomain))
        {
            tenant.SetCustomDomain(request.CustomDomain);
        }

        var settings = new TenantSettings(tenant.Id);
        tenant.SetSettings(settings);

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new TenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.CustomDomain,
            tenant.Status.ToString());

        return Result<TenantResponse>.Success(response);
    }
}

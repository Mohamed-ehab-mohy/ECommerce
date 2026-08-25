using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Platform.Commands;
using ECommerce.UseCases.Tenants.Ports;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.UseCases.Platform.Handlers;

internal sealed class PlatformTenantCommandHandlers(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SuspendTenantCommand, Result>,
      IRequestHandler<ActivateTenantCommand, Result>
{
    public async Task<Result> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(new Error("Tenant.NotFound", "The specified tenant was not found."));
        }

        tenant.Suspend();
        // Note: I will add Suspend() to Tenant.cs next

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(new Error("Tenant.NotFound", "The specified tenant was not found."));
        }

        tenant.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

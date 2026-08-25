using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Tenants.Commands;
using ECommerce.UseCases.Tenants.Ports;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class HandleSubscriptionUpdatedCommandHandler()
    : IRequestHandler<HandleSubscriptionUpdatedCommand, Result>
{
    public async Task<Result> Handle(HandleSubscriptionUpdatedCommand request, CancellationToken cancellationToken)
    {
        // In a real scenario, we might need a method GetTenantByStripeCustomerId in the repository.
        // Here, assuming we fetch it:
        await Task.CompletedTask;
        return Result.Success();
    }
}
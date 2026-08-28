using ECommerce.Domain.Tenants;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Tenants.Commands;
using ECommerce.UseCases.Tenants.Ports;
using MediatR;

namespace ECommerce.UseCases.Tenants.Handlers;

internal sealed class HandleSubscriptionUpdatedCommandHandler(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<HandleSubscriptionUpdatedCommand, Result>
{
    public async Task<Result> Handle(HandleSubscriptionUpdatedCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StripeCustomerId))
        {
            return Result.Failure(new Error("Subscription.MissingCustomer", "Stripe customer id is required."));
        }

        var subscription = await tenantRepository.GetSubscriptionByStripeCustomerIdAsync(request.StripeCustomerId, cancellationToken);
        if (subscription is null)
        {
            return Result.Failure(new Error("Subscription.NotFound", "No subscription is linked to the provided Stripe customer."));
        }

        var periodEnd = DateTimeOffset.FromUnixTimeSeconds(request.CurrentPeriodEndEpoch).UtcDateTime;

        switch (request.Status)
        {
            case "active":
            case "trialing":
                subscription.MarkAsActive(request.StripeCustomerId, request.StripeSubscriptionId, periodEnd);
                break;
            case "past_due":
            case "unpaid":
            case "incomplete":
            case "incomplete_expired":
                subscription.MarkAsPastDue();
                break;
            case "canceled":
            case "unpaid_expired":
            case "paused":
                subscription.Cancel();
                break;
            default:
                // Unknown state: leave the subscription untouched but do not fail the delivery.
                return Result.Success();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Tenants.Commands;

public sealed record HandleSubscriptionUpdatedCommand(
    string StripeCustomerId,
    string StripeSubscriptionId,
    string Status,
    long CurrentPeriodEndEpoch) : IRequest<Result>;

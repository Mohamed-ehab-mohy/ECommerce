using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record HandleStripeRefundCommand(
    string PaymentIntentId,
    decimal AmountRefunded,
    string Reason,
    string? ChargeId) : IRequest<Result>;

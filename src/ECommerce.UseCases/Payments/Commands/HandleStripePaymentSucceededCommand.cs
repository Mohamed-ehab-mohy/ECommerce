using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record HandleStripePaymentSucceededCommand(string PaymentIntentId) : IRequest<Result>;

using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record HandleStripePaymentFailedCommand(string PaymentIntentId) : IRequest<Result>;

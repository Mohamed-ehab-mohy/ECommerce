using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record AuthorizePaymentCommand(Guid PaymentId) : IRequest<Result<PaymentResponse>>;

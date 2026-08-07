using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Payments.Responses;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

public sealed record AuthorizePaymentCommand(Guid PaymentId) : IRequest<Result<PaymentResponse>>;

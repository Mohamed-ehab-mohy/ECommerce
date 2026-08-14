using FluentValidation;

namespace ECommerce.UseCases.Payments.Commands;

public sealed class RequestRefundCommandValidator : AbstractValidator<RequestRefundCommand>
{
    public RequestRefundCommandValidator()
    {
        RuleFor(command => command.PaymentId).NotEmpty();
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

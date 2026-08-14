using FluentValidation;

namespace ECommerce.UseCases.Payments.Commands;

public sealed class CompleteRefundCommandValidator : AbstractValidator<CompleteRefundCommand>
{
    public CompleteRefundCommandValidator()
    {
        RuleFor(command => command.PaymentId).NotEmpty();
    }
}

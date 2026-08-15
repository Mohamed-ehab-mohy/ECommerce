using FluentValidation;

namespace ECommerce.UseCases.Payments.Commands;

public sealed class ExecuteRefundCommandValidator : AbstractValidator<ExecuteRefundCommand>
{
    public ExecuteRefundCommandValidator()
    {
        RuleFor(command => command.RefundId).NotEmpty();
    }
}

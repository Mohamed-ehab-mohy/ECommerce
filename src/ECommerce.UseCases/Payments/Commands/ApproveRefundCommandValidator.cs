using FluentValidation;

namespace ECommerce.UseCases.Payments.Commands;

public sealed class ApproveRefundCommandValidator : AbstractValidator<ApproveRefundCommand>
{
    public ApproveRefundCommandValidator()
    {
        RuleFor(command => command.RefundId).NotEmpty();
    }
}

using FluentValidation;

namespace ECommerce.UseCases.Orders.Commands;

public sealed class ReorderOrderCommandValidator : AbstractValidator<ReorderOrderCommand>
{
    public ReorderOrderCommandValidator()
    {
        RuleFor(command => command.OrderNumber).NotEmpty().MaximumLength(24);
        RuleFor(command => command.RequesterCustomerId).NotEmpty();
    }
}

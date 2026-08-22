
namespace ECommerce.UseCases.Orders.Commands;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(command => command.OrderNumber).NotEmpty().MaximumLength(24);
        RuleFor(command => command.Reason).MaximumLength(400);
    }
}

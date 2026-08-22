
namespace ECommerce.UseCases.Orders.Commands;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(command => command.CheckoutId).NotEmpty();
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);
    }
}

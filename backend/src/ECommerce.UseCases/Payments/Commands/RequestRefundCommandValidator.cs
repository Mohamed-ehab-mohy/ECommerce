
namespace ECommerce.UseCases.Payments.Commands;

public sealed class RequestRefundCommandValidator : AbstractValidator<RequestRefundCommand>
{
    public RequestRefundCommandValidator()
    {
        RuleFor(command => command.OrderNumber).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Amount).GreaterThan(0m);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleForEach(command => command.Items)
            .ChildRules(item =>
            {
                item.RuleFor(line => line.ProductId).NotEmpty();
                item.RuleFor(line => line.Quantity).InclusiveBetween(1, 999);
            });
    }
}

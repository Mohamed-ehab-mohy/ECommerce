
namespace ECommerce.UseCases.Payments.Commands;

public sealed class AuthorizePaymentCommandValidator : AbstractValidator<AuthorizePaymentCommand>
{
    public AuthorizePaymentCommandValidator()
    {
        RuleFor(command => command.PaymentId).NotEmpty();
    }
}

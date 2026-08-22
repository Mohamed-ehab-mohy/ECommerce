
namespace ECommerce.UseCases.Checkout.Commands;

public sealed class InitiateCheckoutCommandValidator : AbstractValidator<InitiateCheckoutCommand>
{
    public InitiateCheckoutCommandValidator()
    {
        RuleFor(command => command.CartId).NotEmpty();
        RuleFor(command => command.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.ShippingAddress).NotNull();
        RuleFor(command => command.ShippingAddress.FullName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.ShippingAddress.Country).NotEmpty().Length(2);
        RuleFor(command => command.ShippingAddress.Street).NotEmpty().MaximumLength(255);
        RuleFor(command => command.ShippingAddress.City).NotEmpty().MaximumLength(120);
        RuleFor(command => command.ShippingMethodId).NotEmpty().MaximumLength(64);
        RuleFor(command => command.ProviderKey).NotEmpty().MaximumLength(30);
        RuleFor(command => command.MethodType).NotEmpty().MaximumLength(20);
    }
}

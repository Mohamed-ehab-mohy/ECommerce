using ECommerce.UseCases.Cart.Queries;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Cart.Queries;

public sealed class GetCartQueryValidator : AbstractValidator<GetCartQuery>
{
    public GetCartQueryValidator(ICurrencyCatalog currencies)
    {
        RuleFor(query => query.OwnerKey).NotEmpty().MaximumLength(64);
        RuleFor(query => query.Currency)
            .Must(currencies.IsSupported)
            .WithMessage("'{PropertyValue}' is not a supported currency.");
    }
}

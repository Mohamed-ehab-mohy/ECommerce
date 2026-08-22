using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Queries;
using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UseCases.Fulfillment.Queries;

public sealed record QuoteShippingRateQuery(
    string DestinationCountry,
    string DestinationPostalCode,
    int WeightGrams,
    string Currency) : IRequest<Result<RateQuoteResponse>>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentRead;
}

public sealed class QuoteShippingRateQueryValidator : AbstractValidator<QuoteShippingRateQuery>
{
    public QuoteShippingRateQueryValidator()
    {
        RuleFor(x => x.DestinationCountry).NotEmpty().Length(2);
        RuleFor(x => x.DestinationPostalCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.WeightGrams).GreaterThan(0).LessThan(2_000_000);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

using ECommerce.Domain.Fulfillment;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Queries;
using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class QuoteShippingRateQueryHandler(
    CarrierRateSelector selector,
    IValidator<QuoteShippingRateQuery> validator) : IRequestHandler<QuoteShippingRateQuery, Result<RateQuoteResponse>>
{
    public async Task<Result<RateQuoteResponse>> Handle(QuoteShippingRateQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<RateQuoteResponse>();
        }

        var selection = await selector.SelectAsync(
            new CarrierShipmentRequest(
                ShippingConstants.OriginCountry,
                request.DestinationCountry.Trim().ToUpperInvariant(),
                request.DestinationPostalCode.Trim().ToUpperInvariant(),
                request.WeightGrams,
                request.Currency.Trim().ToUpperInvariant(),
                []),
            cancellationToken);

        return selection.Cheapest is null
            ? Result<RateQuoteResponse>.Failure(FulfillmentErrors.CarrierUnavailable)
            : Result<RateQuoteResponse>.Success(new RateQuoteResponse(
                selection.Cheapest.CarrierKey,
                selection.Cheapest.Amount,
                selection.Cheapest.Currency,
                selection.Cheapest.EstimatedDeliveryUtc,
                selection.IsFallback,
                selection.FromCache,
                selection.UnavailableCarriers));
    }
}

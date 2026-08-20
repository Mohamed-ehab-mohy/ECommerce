using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.UseCases.Fulfillment.Shipping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Shipping;

public sealed class AramexCarrierAdapter(
    HttpClient httpClient,
    IOptions<CarrierOptions> options,
    TimeProvider timeProvider,
    ILogger<AramexCarrierAdapter> logger) : ICarrierAdapter
{
    public const string Key = "aramex";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string CarrierKey => Key;

    public async Task<CarrierQuoteResult> QuoteAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value.Aramex;
        if (!config.Enabled || string.IsNullOrEmpty(config.ApiKey))
        {
            return FallbackQuote(request);
        }

        try
        {
            var payload = new
            {
                ClientInfo = new { UserName = config.ApiKey, Password = config.ApiSecret, AccountNumber = config.AccountNumber },
                OriginAddress = new { CountryCode = request.OriginCountry },
                DestinationAddress = new { CountryCode = request.DestinationCountry, PostCode = request.DestinationPostalCode },
                ShipmentDetails = new { Weight = request.WeightGrams / 1000.0, Unit = "KG" },
                Currency = request.Currency
            };

            var response = await httpClient.PostAsJsonAsync(
                $"{config.BaseUrl}/shipping/rate", payload, JsonOptions, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AramexRateResponse>(JsonOptions, cancellationToken);

            var amount = result?.TotalAmount ?? 15m;
            var estimatedDelivery = timeProvider.GetUtcNow().UtcDateTime.AddDays(4);

            return new CarrierQuoteResult(Key, amount, request.Currency, estimatedDelivery);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Aramex rate API failed, using fallback");
            return FallbackQuote(request);
        }
    }

    public async Task<CarrierShipmentResult> CreateShipmentAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value.Aramex;
        if (!config.Enabled || string.IsNullOrEmpty(config.ApiKey))
        {
            return FallbackShipment();
        }

        try
        {
            var payload = new
            {
                ClientInfo = new { UserName = config.ApiKey, Password = config.ApiSecret, AccountNumber = config.AccountNumber },
                OriginAddress = new { CountryCode = request.OriginCountry },
                DestinationAddress = new { CountryCode = request.DestinationCountry, PostCode = request.DestinationPostalCode },
                ShipmentDetails = new { Weight = request.WeightGrams / 1000.0, Unit = "KG" },
                Currency = request.Currency
            };

            var response = await httpClient.PostAsJsonAsync(
                $"{config.BaseUrl}/shipping/shipment", payload, JsonOptions, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AramexShipmentResponse>(JsonOptions, cancellationToken);

            var trackingNumber = result?.TrackingNumber ?? $"ARX{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
            var labelUrl = result?.LabelUrl ?? $"https://portal.aramex.com/labels/{trackingNumber}.pdf";

            return new CarrierShipmentResult(Key, trackingNumber, labelUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Aramex shipment API failed, using fallback");
            return FallbackShipment();
        }
    }

    private static CarrierQuoteResult FallbackQuote(CarrierShipmentRequest request)
    {
        var units = (int)Math.Ceiling(request.WeightGrams / 100.0);
        var amount = 15m + units * 1.2m;
        return new CarrierQuoteResult(Key, amount, request.Currency, DateTime.UtcNow.AddDays(4));
    }

    private static CarrierShipmentResult FallbackShipment()
    {
        var trackingNumber = $"ARX{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
        return new CarrierShipmentResult(Key, trackingNumber, $"https://portal.aramex.com/labels/{trackingNumber}.pdf");
    }

    private sealed record AramexRateResponse(decimal TotalAmount);
    private sealed record AramexShipmentResponse(string TrackingNumber, string LabelUrl);
}

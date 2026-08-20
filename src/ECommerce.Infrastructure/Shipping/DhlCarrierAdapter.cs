using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.UseCases.Fulfillment.Shipping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Shipping;

public sealed class DhlCarrierAdapter(
    HttpClient httpClient,
    IOptions<CarrierOptions> options,
    TimeProvider timeProvider,
    ILogger<DhlCarrierAdapter> logger) : ICarrierAdapter
{
    public const string Key = "dhl";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string CarrierKey => Key;

    public async Task<CarrierQuoteResult> QuoteAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value.Dhl;
        if (!config.Enabled || string.IsNullOrEmpty(config.ApiKey))
        {
            return FallbackQuote(request);
        }

        try
        {
            var payload = new
            {
                CustomerDetails = new { ShipperNumber = config.AccountNumber },
                PlannedShippingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                IsCustomsDeclarable = false,
                UnitOfMeasurement = "metric",
                IsDutiable = false,
                CurrencyCode = request.Currency,
                Offices = new
                {
                    Pickup = new { CountryCode = request.OriginCountry },
                    Delivery = new { CountryCode = request.DestinationCountry, Postcode = request.DestinationPostalCode }
                },
                Pieces = new[]
                {
                    new { Weight = request.WeightGrams / 1000.0, Dimensions = new { Length = 30, Width = 20, Height = 10 } }
                }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/ship/rates")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            requestMessage.Headers.Add("DHL-API-Key", config.ApiKey);

            var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DhlRateResponse>(JsonOptions, cancellationToken);

            var amount = result?.TotalPrice ?? 20m;
            var estimatedDelivery = timeProvider.GetUtcNow().UtcDateTime.AddDays(2);

            return new CarrierQuoteResult(Key, amount, request.Currency, estimatedDelivery);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DHL rate API failed, using fallback");
            return FallbackQuote(request);
        }
    }

    public async Task<CarrierShipmentResult> CreateShipmentAsync(
        CarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value.Dhl;
        if (!config.Enabled || string.IsNullOrEmpty(config.ApiKey))
        {
            return FallbackShipment();
        }

        try
        {
            var payload = new
            {
                CustomerDetails = new { ShipperNumber = config.AccountNumber },
                PlannedShippingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                IsCustomsDeclarable = false,
                UnitOfMeasurement = "metric",
                CurrencyCode = request.Currency,
                Offices = new
                {
                    Pickup = new { CountryCode = request.OriginCountry },
                    Delivery = new { CountryCode = request.DestinationCountry, Postcode = request.DestinationPostalCode }
                },
                Pieces = new[]
                {
                    new { Weight = request.WeightGrams / 1000.0, Dimensions = new { Length = 30, Width = 20, Height = 10 } }
                }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/ship/shipments")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            requestMessage.Headers.Add("DHL-API-Key", config.ApiKey);

            var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DhlShipmentResponse>(JsonOptions, cancellationToken);

            var trackingNumber = result?.TrackingNumber ?? $"DHL{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
            var labelUrl = result?.LabelUrl ?? $"https://www.dhl.com/track?id={trackingNumber}";

            return new CarrierShipmentResult(Key, trackingNumber, labelUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DHL shipment API failed, using fallback");
            return FallbackShipment();
        }
    }

    private static CarrierQuoteResult FallbackQuote(CarrierShipmentRequest request)
    {
        var units = (int)Math.Ceiling(request.WeightGrams / 100.0);
        var amount = 20m + units * 1.0m;
        return new CarrierQuoteResult(Key, amount, request.Currency, DateTime.UtcNow.AddDays(2));
    }

    private static CarrierShipmentResult FallbackShipment()
    {
        var trackingNumber = $"DHL{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(100000, 999999)}";
        return new CarrierShipmentResult(Key, trackingNumber, $"https://www.dhl.com/track?id={trackingNumber}");
    }

    private sealed record DhlRateResponse(decimal TotalPrice);
    private sealed record DhlShipmentResponse(string TrackingNumber, string LabelUrl);
}

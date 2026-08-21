using ECommerce.UseCases.Pricing;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/currencies")]
public sealed class CurrenciesController(ICurrencyExchangeService exchangeService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetSupportedCurrencies(CancellationToken cancellationToken)
    {
        var currencies = await exchangeService.GetSupportedCurrenciesAsync(cancellationToken);
        return Ok(currencies);
    }

    [HttpGet("rates")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetRates(
        [FromQuery] string baseCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        var rates = await exchangeService.GetAllRatesAsync(baseCurrency, cancellationToken);
        return Ok(new { baseCurrency, rates });
    }

    [HttpGet("convert")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> Convert(
        [FromQuery] decimal amount,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken cancellationToken)
    {
        var converted = await exchangeService.ConvertAsync(amount, from, to, cancellationToken);
        return Ok(new
        {
            originalAmount = amount,
            fromCurrency = from.ToUpperInvariant(),
            toCurrency = to.ToUpperInvariant(),
            convertedAmount = converted
        });
    }
}

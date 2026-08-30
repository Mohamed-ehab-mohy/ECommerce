using ECommerce.API.Common;
using ECommerce.UseCases.Checkout.Commands;
using ECommerce.UseCases.Checkout.Queries;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/guest-checkout")]
public sealed class GuestCheckoutController(ISender sender) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Initiate(
        GuestInitiateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new InitiateCheckoutCommand(
                request.CartId,
                CustomerId: null,
                request.CustomerEmail,
                request.Currency,
                new AddressInput(
                    request.ShippingAddress.FullName,
                    request.ShippingAddress.Phone,
                    request.ShippingAddress.Street,
                    request.ShippingAddress.City,
                    request.ShippingAddress.Region,
                    request.ShippingAddress.Country,
                    request.ShippingAddress.PostalCode),
                request.BillingAddress is null
                    ? null
                    : new AddressInput(
                        request.BillingAddress.FullName,
                        request.BillingAddress.Phone,
                        request.BillingAddress.Street,
                        request.BillingAddress.City,
                        request.BillingAddress.Region,
                        request.BillingAddress.Country,
                        request.BillingAddress.PostalCode),
                request.ShippingMethodId,
                request.PaymentMethod.ProviderKey,
                request.PaymentMethod.MethodType,
                request.ShippingAddress.Country),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : CreatedAtAction(nameof(Get), new { id = result.Value.CheckoutId }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCheckoutQuery(id), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("{checkoutId:guid}/place")]
    [AllowAnonymous]
    public async Task<IActionResult> Place(
        Guid checkoutId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Checkout-Token")] string? capabilityToken,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PlaceOrderCommand(
                checkoutId,
                idempotencyKey ?? string.Empty,
                capabilityToken ?? string.Empty),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("orders")]
    [Authorize]
    public async Task<IActionResult> GetOrdersByEmail(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrdersByEmailQuery(email), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

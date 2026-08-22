using ECommerce.API.Common;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Cart.Queries;
using ECommerce.UseCases.Common;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/carts/me")]
public sealed class CartController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    private const string CartKeyHeader = "X-Cart-Key";

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new GetCartQuery(ownerKey, currency ?? "USD"),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("items")]
    public async Task<IActionResult> Add(
        AddCartItemRequest request,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new AddCartItemCommand(ownerKey, currency ?? "USD", request.ProductId, request.Quantity),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPatch("items/{productId:guid}")]
    public async Task<IActionResult> Update(
        Guid productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new UpdateCartItemCommand(ownerKey, productId, request.Quantity),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> Remove(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new RemoveCartItemCommand(ownerKey, productId),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("price-changes")]
    public async Task<IActionResult> PriceChanges(CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new GetCartPriceChangesQuery(ownerKey),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("coupons")]
    public async Task<IActionResult> ApplyCoupon(
        ApplyCartCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new ApplyCartCouponCommand(ownerKey, request.Code),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpDelete("coupons")]
    public async Task<IActionResult> RemoveCoupon(CancellationToken cancellationToken = default)
    {
        var (ownerKey, issuedKey) = ResolveOwnerKey();

        var result = await sender.Send(
            new RemoveCartCouponCommand(ownerKey),
            cancellationToken);

        if (issuedKey is not null)
        {
            Response.Headers[CartKeyHeader] = issuedKey;
        }

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    private (string OwnerKey, string? IssuedKey) ResolveOwnerKey()
    {
        if (currentUser.UserId is { } userId)
        {
            return ($"user:{userId}", null);
        }

        var cartKey = Request.Headers[CartKeyHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cartKey))
        {
            return ($"anon:{cartKey}", null);
        }

        var issued = Guid.NewGuid().ToString("N");
        return ($"anon:{issued}", issued);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wishlist.Commands;
using ECommerce.UseCases.Wishlist.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/wishlist")]
[Authorize]
public sealed class WishlistController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    private string OwnerKey =>
        currentUser.UserId is { } userId
            ? $"user:{userId}"
            : throw new UnauthorizedAccessException("Wishlist requires an authenticated user.");

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWishlistQuery(OwnerKey), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("items")]
    public async Task<IActionResult> Add(AddWishlistItemRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddWishlistItemCommand(OwnerKey, request.ProductId),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveWishlistItemCommand(OwnerKey, productId),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("items/{productId:guid}/move")]
    public async Task<IActionResult> Move(Guid productId, [FromQuery] string? currency, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MoveWishlistItemToCartCommand(OwnerKey, productId, currency ?? "USD"),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

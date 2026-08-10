using ECommerce.API.Common;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    private const int DefaultPageSize = 20;

    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<IActionResult> History(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } customerId)
        {
            return Unauthorized();
        }

        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await sender.Send(
            new GetOrderHistoryQuery(customerId, cursor, pageSize),
            cancellationToken);

        if (result.IsFailure)
        {
            return ProblemResponse.Create(result.ToOperationError());
        }

        if (result.Value.NextCursor is { } nextCursor)
        {
            var next = $"{Request.Path}?cursor={Uri.EscapeDataString(nextCursor)}&pageSize={pageSize}";
            Response.Headers["Link"] = $"<{next}>; rel=\"next\"";
        }

        return Ok(result.Value);
    }

    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> Detail(string orderNumber, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOrderQuery(
                orderNumber,
                currentUser.UserId,
                currentUser.Permissions.Contains(Permissions.OrdersSupportRead, StringComparer.Ordinal)),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{orderNumber}/timeline")]
    public async Task<IActionResult> Timeline(string orderNumber, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOrderQuery(
                orderNumber,
                currentUser.UserId,
                currentUser.Permissions.Contains(Permissions.OrdersSupportRead, StringComparer.Ordinal)),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value.Timeline);
    }

    [HttpPost("{orderNumber}/cancel")]
    public async Task<IActionResult> Cancel(
        string orderNumber,
        CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelOrderCommand(
                orderNumber,
                request.Reason,
                currentUser.UserId,
                currentUser.Permissions.Contains(Permissions.OrdersSupportRead, StringComparer.Ordinal)),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

public sealed record CancelOrderRequest(string? Reason);


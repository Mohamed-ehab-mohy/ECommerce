using ECommerce.API.Controllers;
using ECommerce.UseCases.Wallets.Commands;
using ECommerce.UseCases.Wallets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ECommerce.UseCases.Common;
using ECommerce.API.Common;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/wallets")]
public sealed class WalletsController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyWallet(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyWalletQuery(), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    public sealed record DepositRequest(decimal Amount);

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DepositToWalletCommand(request.Amount), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok();
    }

    public sealed record ConvertPointsRequest(int Points);

    [HttpPost("convert-points")]
    public async Task<IActionResult> ConvertPoints([FromBody] ConvertPointsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConvertPointsCommand(request.Points), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok();
    }

    private static IActionResult ToProblem(ECommerce.UseCases.Common.OperationError error) => ProblemResponse.Create(error);
}

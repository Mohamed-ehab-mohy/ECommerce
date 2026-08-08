using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Flags.Commands;
using ECommerce.UseCases.Flags.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/flags")]
public sealed class FeatureFlagsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListFeatureFlagsQuery(), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFeatureFlagQuery(key), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Set(string key, SetFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetFeatureFlagCommand(key, request.Enabled), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

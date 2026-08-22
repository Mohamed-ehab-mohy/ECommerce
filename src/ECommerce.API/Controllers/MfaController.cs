using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/mfa")]
public sealed class MfaController(ISender sender) : ControllerBase
{
    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetupMfaCommand(User.GetUserId()), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyMfaRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyMfaCommand(User.GetUserId(), request.Code), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(new { verified = true });
    }
}

public sealed record VerifyMfaRequest(string Code);

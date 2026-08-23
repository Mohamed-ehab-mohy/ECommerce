using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Notifications.Commands;
using ECommerce.UseCases.Notifications.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/me/notifications")]
public sealed class NotificationPreferencesController(ISender sender) : ControllerBase
{
    [HttpGet("preferences")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListNotificationPreferencesQuery(User.GetUserId()),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [HttpPut("preferences/{channel}/{kind}")]
    public async Task<IActionResult> Update(
        string channel,
        string kind,
        UpdateNotificationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateNotificationPreferenceCommand(
            User.GetUserId(),
            channel,
            kind,
            request.Enabled), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

using ECommerce.API.Common;
using ECommerce.UseCases.Audit.Queries;
using ECommerce.UseCases.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Audit;

[ApiController]
[Authorize(Policy = "AuditRead")]
[Route("api/v1/audit-logs")]
public sealed class AuditController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAuditLogsQuery(actorId, action, entityType, from, to, page, pageSize),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

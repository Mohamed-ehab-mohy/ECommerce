using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reconciliation")]
public sealed class ReconciliationController(ISender sender) : ControllerBase
{
    /// <summary>Triggers a provider-vs-platform reconciliation run (finance.reconcile).</summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RunReconciliationCommand(), cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

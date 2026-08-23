using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Commands;
using ECommerce.UseCases.Promotions.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/promotions")]
public sealed class PromotionsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetPromotionsQuery(), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePromotionCommand(
            request.Name,
            request.Conditions ?? [],
            request.Actions ?? [],
            request.AllowStack,
            request.AllowStackWith ?? [],
            request.EligibleCountries ?? [],
            request.EligibleCurrencies ?? [],
            request.StartsAt,
            request.EndsAt), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [Authorize]
    [HttpPatch("{promotionId:guid}")]
    public async Task<IActionResult> Update(Guid promotionId, UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePromotionCommand(
            promotionId,
            request.Name,
            request.Conditions ?? [],
            request.Actions ?? [],
            request.AllowStack,
            request.AllowStackWith ?? [],
            request.EligibleCountries ?? [],
            request.EligibleCurrencies ?? []), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{promotionId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ActivatePromotionCommand(promotionId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{promotionId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new PausePromotionCommand(promotionId), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("{promotionId:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid promotionId, SchedulePromotionRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new SchedulePromotionCommand(promotionId, request.StartsAt, request.EndsAt),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

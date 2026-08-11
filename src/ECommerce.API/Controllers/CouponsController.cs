using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Coupons.Commands;
using ECommerce.UseCases.Coupons.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/coupons")]
public sealed class CouponsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetCouponsQuery(), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(result.Value);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCouponRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCouponCommand(
            request.Code,
            request.PromotionId,
            request.TotalUses,
            request.PerCustomerLimit,
            request.StartsAt,
            request.EndsAt), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);
}

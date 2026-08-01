using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCommand(
            request.Email,
            request.Password,
            request.DisplayName,
            request.Locale,
            request.Currency), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status201Created, new
            {
                userId = result.Value,
                status = "pendingVerification",
                message = "Verification email sent."
            });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyEmailCommand(request.Token), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(new { status = "verified" });
    }

    private static IActionResult ToProblem(OperationError error)
    {
        var problem = new ProblemDetails
        {
            Status = error.StatusCode,
            Type = error.Type,
            Title = Title(error.StatusCode),
            Detail = error.Detail
        };

        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem)
        {
            StatusCode = error.StatusCode
        };
    }

    private static string Title(int statusCode) => statusCode switch
    {
        422 => "Validation Failed",
        409 => "Conflict",
        404 => "Not Found",
        401 => "Unauthorized",
        403 => "Forbidden",
        _ => "Internal Server Error"
    };
}

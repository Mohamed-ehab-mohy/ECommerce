using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        [FromHeader(Name = "X-Device-Id")] string? deviceId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password, deviceId ?? "unknown"),
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(ToTokenResponse(result.Value));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshCommand(request.RefreshToken), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(ToTokenResponse(result.Value));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutAllCommand(User.GetUserId()), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : NoContent();
    }

    private static object ToTokenResponse(LoginResult result) => new
    {
        accessToken = result.AccessToken,
        refreshToken = result.RefreshToken,
        expiresIn = result.ExpiresInSeconds,
        tokenType = "Bearer",
        user = new
        {
            id = result.UserId,
            email = result.Email,
            roles = result.Roles
        }
    };

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

        if (error.RetryAfterSeconds is { } retryAfter)
        {
            problem.Extensions["retryAfter"] = retryAfter;
        }

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
        423 => "Locked",
        429 => "Too Many Requests",
        _ => "Internal Server Error"
    };
}

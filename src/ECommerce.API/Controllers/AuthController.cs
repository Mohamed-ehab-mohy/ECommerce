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
            new LoginCommand(
                request.Email,
                request.Password,
                deviceId ?? "unknown",
                ResolveClientIp(HttpContext)),
            cancellationToken);

        if (result.IsFailure)
        {
            var problem = result.ToOperationError();

            if (problem.RetryAfterSeconds is { } retryAfter)
            {
                Response.Headers.RetryAfter = retryAfter.ToString();
            }

            return ToProblem(problem);
        }

        return Ok(ToTokenResponse(result.Value));
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : StatusCode(StatusCodes.Status202Accepted, new { status = "accepted" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);

        return result.IsFailure
            ? ToProblem(result.ToOperationError())
            : Ok(new { status = "passwordReset" });
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

    private static IActionResult ToProblem(OperationError error) => ProblemResponse.Create(error);

    private static string ResolveClientIp(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress?.ToString();
        var isLoopback = remote is null or "::1" or "127.0.0.1";

        if (!isLoopback && !string.IsNullOrWhiteSpace(remote))
        {
            return remote;
        }

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return string.IsNullOrWhiteSpace(remote) ? "unknown" : remote;
    }
}

using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Commands;
using Microsoft.AspNetCore.Authorization;

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
        [FromHeader(Name = "X-Cart-Key")] string? cartKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginCommand(
                request.Email,
                request.Password,
                deviceId ?? "unknown",
                ClientIpResolver.Resolve(HttpContext),
                string.IsNullOrWhiteSpace(cartKey) ? null : cartKey),
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

    [Authorize]
    [HttpPost("impersonate")]
    public async Task<IActionResult> Impersonate(ImpersonateRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ImpersonateUserCommand(request.TargetUserId), cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.ToOperationError());
        }

        var r = result.Value;
        return Ok(new
        {
            accessToken = r.AccessToken,
            refreshToken = r.RefreshToken,
            expiresIn = r.ExpiresInSeconds,
            tokenType = "Bearer",
            impersonatorId = r.ImpersonatorId,
            user = new
            {
                id = r.UserId,
                email = r.Email,
                roles = r.Roles
            }
        });
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
}

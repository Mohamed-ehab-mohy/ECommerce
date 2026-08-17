using ECommerce.API.Common;
using ECommerce.Infrastructure.Identity;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/auth/oauth")]
public sealed class OAuthController(
    ISender sender) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.GrantType))
        {
            return BadRequest(new { error = "invalid_request", error_description = "grant_type is required." });
        }

        return request.GrantType switch
        {
            "client_credentials" => await HandleClientCredentialsAsync(request, cancellationToken),
            "password" => await HandlePasswordAsync(request, cancellationToken),
            "authorization_code" => StatusCode(StatusCodes.Status501NotImplemented,
                new { error = "unsupported_grant_type", error_description = "authorization_code grant is not yet implemented." }),
            _ => BadRequest(new { error = "unsupported_grant_type", error_description = $"Grant type '{request.GrantType}' is not supported." })
        };
    }

    [HttpPost("revoke")]
    public IActionResult Revoke([FromForm] OAuthRevokeRequest request)
    {
        return NoContent();
    }

    [HttpGet(".well-known/openid-configuration")]
    public IActionResult GetDiscoveryDocument([FromServices] JwtOptions jwtOptions)
    {
        return Ok(new
        {
            issuer = jwtOptions.Issuer,
            token_endpoint = $"{jwtOptions.Issuer}/api/v1/auth/oauth/token",
            revocation_endpoint = $"{jwtOptions.Issuer}/api/v1/auth/oauth/revoke",
            response_types_supported = new[] { "code", "token" },
            grant_types_supported = new[] { "authorization_code", "client_credentials", "password" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            scopes_supported = new[] { "openid", "profile", "email", "orders.read", "orders.write", "catalog.read" }
        });
    }

    private async Task<IActionResult> HandleClientCredentialsAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return BadRequest(new { error = "invalid_client", error_description = "client_id and client_secret are required." });
        }

        var result = await sender.Send(
            new ClientCredentialsTokenCommand(request.ClientId, request.ClientSecret, request.Scope),
            cancellationToken);

        return !result.IsSuccess
            ? MapError(result.Error!)
            : Ok(new
            {
                access_token = result.Value!.AccessToken,
                token_type = result.Value!.TokenType,
                expires_in = result.Value!.ExpiresInSeconds,
                scope = result.Value!.Scope
            });
    }

    private async Task<IActionResult> HandlePasswordAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return BadRequest(new { error = "invalid_client", error_description = "client_id and client_secret are required." });
        }

        var result = await sender.Send(
            new PasswordTokenCommand(request.ClientId, request.ClientSecret, request.Username ?? string.Empty, request.Password ?? string.Empty, request.Scope),
            cancellationToken);

        return !result.IsSuccess
            ? MapError(result.Error!)
            : Ok(new
            {
                access_token = result.Value!.AccessToken,
                token_type = result.Value!.TokenType,
                expires_in = result.Value!.ExpiresInSeconds,
                scope = result.Value!.Scope
            });
    }

    private static IActionResult MapError(Shared.Errors.Error error) => error.Type switch
    {
        Shared.Errors.ErrorType.Unauthorized => new UnauthorizedObjectResult(new { error = "invalid_client", error_description = error.Description }),
        Shared.Errors.ErrorType.BadRequest => new BadRequestObjectResult(new { error = "invalid_request", error_description = error.Description }),
        _ => new ObjectResult(new { error = "server_error", error_description = error.Description }) { StatusCode = 500 }
    };
}

using System.IdentityModel.Tokens.Jwt;
using ECommerce.API.Common;
using ECommerce.Infrastructure.Identity;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/auth/oauth")]
public sealed class OAuthController(
    ISender sender,
    IDistributedCache cache,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Issue an access token via client_credentials or password grant.</summary>
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
            "authorization_code" => await HandleAuthorizationCodeAsync(request, cancellationToken),
            _ => BadRequest(new { error = "unsupported_grant_type", error_description = $"Grant type '{request.GrantType}' is not supported." })
        };
    }

    /// <summary>
    /// Issue a short-lived, single-use authorization code for an already-authenticated user.
    /// The requesting client must present a valid user Bearer token; the code is bound to the
    /// authenticated user, the client, the redirect_uri, the requested scopes and an optional
    /// PKCE code_challenge. Exchange it at the token endpoint with grant_type=authorization_code.
    /// </summary>
    [Authorize]
    [HttpPost("authorize")]
    public async Task<IActionResult> Authorize([FromForm] OAuthAuthorizeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return BadRequest(new { error = "invalid_request", error_description = "client_id and redirect_uri are required." });
        }

        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Unauthorized(new { error = "invalid_request", error_description = "Authenticated user could not be resolved." });
        }

        var result = await sender.Send(
            new AuthorizeCommand(
                userId.Value,
                request.ClientId,
                request.RedirectUri,
                request.CodeChallenge,
                request.CodeChallengeMethod,
                request.Scope),
            cancellationToken);

        return !result.IsSuccess
            ? MapAuthorizeError(result.Error!)
            : Ok(new { code = result.Value!.Code, redirect_uri = result.Value!.RedirectUri, scope = result.Value.Scope });
    }

    /// <summary>Revoke an issued access token by adding its id (jti) to a Redis blocklist until expiry.</summary>
    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromForm] OAuthRevokeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { error = "invalid_request", error_description = "token is required." });
        }

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(request.Token))
        {
            return BadRequest(new { error = "invalid_request", error_description = "The provided token could not be parsed." });
        }

        JwtSecurityToken jwt;
        try
        {
            jwt = handler.ReadJwtToken(request.Token);
        }
        catch (Exception)
        {
            return BadRequest(new { error = "invalid_request", error_description = "The provided token could not be parsed." });
        }

        var jti = jwt.Id
            ?? jwt.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrWhiteSpace(jti))
        {
            return Ok();
        }

        var expiry = jwt.ValidTo;
        var ttl = expiry <= DateTime.UtcNow
            ? TimeSpan.FromMinutes(1)
            : expiry - DateTime.UtcNow;

        if (ttl > TimeSpan.Zero)
        {
            await cache.SetStringAsync($"jwt:revoked:{jti}", "1", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, cancellationToken);
        }

        return Ok();
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
            new PasswordTokenCommand(
                request.ClientId,
                request.ClientSecret,
                request.Username ?? string.Empty,
                request.Password ?? string.Empty,
                request.Scope,
                ClientIpResolver.Resolve(HttpContext)),
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

    private async Task<IActionResult> HandleAuthorizationCodeAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code)
            || string.IsNullOrWhiteSpace(request.ClientId)
            || string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return BadRequest(new { error = "invalid_request", error_description = "code, client_id and redirect_uri are required." });
        }

        var result = await sender.Send(
            new AuthorizationCodeTokenCommand(
                request.Code,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                request.CodeVerifier,
                request.Scope),
            cancellationToken);

        return !result.IsSuccess
            ? MapAuthorizationCodeError(result.Error!)
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

    private static IActionResult MapAuthorizationCodeError(Shared.Errors.Error error) =>
        error.Code == OAuthErrors.InvalidClient.Code
            ? new UnauthorizedObjectResult(new { error = "invalid_client", error_description = error.Description })
            : error.Code == OAuthErrors.InvalidScope.Code
                ? new BadRequestObjectResult(new { error = "invalid_scope", error_description = error.Description })
                : new BadRequestObjectResult(new { error = "invalid_grant", error_description = error.Description });

    private static IActionResult MapAuthorizeError(Shared.Errors.Error error) =>
        error.Code == OAuthErrors.InvalidClient.Code
            ? new UnauthorizedObjectResult(new { error = "invalid_client", error_description = error.Description })
            : error.Code == OAuthErrors.InvalidScope.Code
                ? new BadRequestObjectResult(new { error = "invalid_scope", error_description = error.Description })
                : new BadRequestObjectResult(new { error = "invalid_request", error_description = error.Description });
}

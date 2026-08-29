using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

public sealed class OAuthTokenRequest
{
    [FromForm(Name = "grant_type")]
    public string GrantType { get; set; } = string.Empty;

    [FromForm(Name = "code")]
    public string? Code { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [FromForm(Name = "username")]
    public string? Username { get; set; }

    [FromForm(Name = "password")]
    public string? Password { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }
}

public sealed class OAuthAuthorizeRequest
{
    [FromForm(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [FromForm(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }
}

public sealed class OAuthRevokeRequest
{
    [FromForm(Name = "token")]
    public string Token { get; set; } = string.Empty;

    [FromForm(Name = "token_hint")]
    public string? TokenHint { get; set; }
}

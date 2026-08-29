using System.Security.Cryptography;
using System.Text;
using ECommerce.Domain.Identity;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UnitTests;

public sealed class OAuthAuthorizationCodeTests
{
    private const string ClientId = "native-app";
    private const string ClientSecret = "s3cr3t";
    private const string RedirectUri = "https://app.example.com/cb";

    private readonly FakeUserRepository _users = new();
    private readonly FakeAccessTokenIssuer _accessTokenIssuer = new();
    private readonly FakeOAuthClientValidator _clientValidator = new();
    private readonly FakeAuthorizationCodeStore _codeStore = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private AuthorizeCommandHandler AuthorizeHandler => new(_clientValidator, _codeStore, _users);

    private AuthorizationCodeTokenHandler TokenHandler =>
        new(_clientValidator, _codeStore, _users, _accessTokenIssuer, _timeProvider);

    [Fact]
    public async Task Authorize_Valid_Request_Issues_Code()
    {
        var customer = CreateCustomer();
        _clientValidator.Client = FakeOAuthClientValidator.Confidential(
            ClientId, scopes: ["openid", "catalog.read"]);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, RedirectUri, null, null, "openid catalog.read"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Code));
        Assert.Equal(RedirectUri, result.Value.RedirectUri);
        Assert.Equal("openid catalog.read", result.Value.Scope);

        var stored = Assert.Single(_codeStore.Records);
        Assert.Equal(customer.Id, stored.UserId);
        Assert.Equal(ClientId, stored.ClientId);
        Assert.Equal(RedirectUri, stored.RedirectUri);
        Assert.Equal(["openid", "catalog.read"], stored.Scopes);
        Assert.Null(stored.CodeChallenge);
    }

    [Fact]
    public async Task Authorize_With_CodeChallenge_Stores_Challenge()
    {
        var customer = CreateCustomer();
        var challenge = "AB12challenge-value";
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, RedirectUri, challenge, "S256", "openid"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_codeStore.Records);
        Assert.Equal(challenge, stored.CodeChallenge);
        Assert.Equal("S256", stored.CodeChallengeMethod);
    }

    [Fact]
    public async Task Authorize_Unknown_Client_Returns_InvalidClient()
    {
        var customer = CreateCustomer();
        _clientValidator.Client = null;

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, RedirectUri, null, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidClient, result.Error);
    }

    [Fact]
    public async Task Authorize_Disallowed_RedirectUri_Returns_InvalidRedirectUri()
    {
        var customer = CreateCustomer();
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, "https://evil.example.com/cb", null, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidRedirectUri, result.Error);
    }

    [Fact]
    public async Task Authorize_Clients_Without_AuthorizationCode_Grant_Are_Rejected()
    {
        var customer = CreateCustomer();
        _clientValidator.Client = FakeOAuthClientValidator.Public(
            ClientId, grantTypes: ["client_credentials"]);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, RedirectUri, null, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.UnauthorizedGrantType, result.Error);
    }

    [Fact]
    public async Task Authorize_Unsupported_CodeChallenge_Method_Returns_InvalidCodeChallenge()
    {
        var customer = CreateCustomer();
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, RedirectUri, "plain-challenge", "plain", "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidCodeChallenge, result.Error);
    }

    [Fact]
    public async Task Authorize_No_Allowed_Scopes_Returns_InvalidScope()
    {
        var customer = CreateCustomer();
        _clientValidator.Client = FakeOAuthClientValidator.Public(
            ClientId, scopes: ["catalog.read"]);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(customer.Id, ClientId, RedirectUri, null, null, "orders.write"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidScope, result.Error);
    }

    [Fact]
    public async Task Authorize_Unknown_User_Returns_InvalidGrant()
    {
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await AuthorizeHandler.Handle(
            new AuthorizeCommand(Guid.NewGuid(), ClientId, RedirectUri, null, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidGrant, result.Error);
    }

    [Fact]
    public async Task Token_Exchange_Valid_Code_Issues_Access_Token()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid", "catalog.read"], null, null);
        _clientValidator.Client = FakeOAuthClientValidator.Confidential(ClientId);
        _clientValidator.SecretValidation = _clientValidator.Client;

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, ClientSecret, RedirectUri, null, "openid catalog.read"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal("openid catalog.read", result.Value.Scope);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.Equal(1, _accessTokenIssuer.IssueCount);
    }

    [Fact]
    public async Task Token_Exchange_Failed_Secret_Returns_InvalidClient()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid"], null, null);
        _clientValidator.Client = FakeOAuthClientValidator.Confidential(ClientId);
        _clientValidator.SecretValidation = null;

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, "wrong", RedirectUri, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidClient, result.Error);
    }

    [Fact]
    public async Task Token_Exchange_Public_Client_Does_Not_Require_Secret()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid"], null, null);
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, null, RedirectUri, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _clientValidator.ValidateAsyncCalls);
    }

    [Fact]
    public async Task Token_Exchange_Consumed_Code_Returns_InvalidGrant()
    {
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("missing-code", ClientId, null, RedirectUri, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidGrant, result.Error);
    }

    [Fact]
    public async Task Token_Exchange_Client_Mismatch_Returns_InvalidGrant()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid"], null, null);
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", "other-client", null, RedirectUri, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidGrant, result.Error);
    }

    [Fact]
    public async Task Token_Exchange_Redirect_Mismatch_Returns_InvalidGrant()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid"], null, null);
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, null, "https://evil.example.com/cb", null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidGrant, result.Error);
    }

    [Fact]
    public async Task Token_Exchange_Pkce_Valid_Verifier_Issues_Token()
    {
        var customer = CreateCustomer();
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        AddCode("code-1", customer.Id, ["openid"], Challenge(verifier), "S256");
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, null, RedirectUri, verifier, "openid"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _accessTokenIssuer.IssueCount);
    }

    [Fact]
    public async Task Token_Exchange_Pkce_Invalid_Verifier_Returns_InvalidPkceVerifier()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid"], Challenge("correct-verifier"), "S256");
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, null, RedirectUri, "wrong-verifier", "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidPkceVerifier, result.Error);
    }

    [Fact]
    public async Task Token_Exchange_Missing_Pkce_Verifier_When_Challenge_Present_Returns_InvalidPkceVerifier()
    {
        var customer = CreateCustomer();
        AddCode("code-1", customer.Id, ["openid"], Challenge("correct-verifier"), "S256");
        _clientValidator.Client = FakeOAuthClientValidator.Public(ClientId);

        var result = await TokenHandler.Handle(
            new AuthorizationCodeTokenCommand("code-1", ClientId, null, RedirectUri, null, "openid"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OAuthErrors.InvalidPkceVerifier, result.Error);
    }

    private Customer CreateCustomer()
    {
        var customer = Customer.Register(
            $"user-{Guid.NewGuid():N}@example.com",
            "Test User",
            "en",
            "USD",
            "hash",
            "verify",
            DateTime.UtcNow.AddHours(24),
            "verify-raw");
        _users.Customers.Add(customer);
        _users.RolesByUser[customer.Id] = [IdentityRoles.Customer];
        _users.PermissionsByUser[customer.Id] = ["catalog.read"];
        return customer;
    }

    private void AddCode(string code, Guid userId, IReadOnlyList<string> scopes, string? challenge, string? method) =>
        _codeStore.Records.Add(new AuthorizationCodeRecord(
            code,
            userId,
            ClientId,
            RedirectUri,
            scopes,
            challenge,
            method));

    private static string Challenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

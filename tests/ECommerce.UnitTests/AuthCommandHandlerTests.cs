using ECommerce.Domain.Audit;
using ECommerce.Shared.Audit;
using ECommerce.Domain.Identity;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;

namespace ECommerce.UnitTests;

public sealed class AuthCommandHandlerTests
{
    private const string Password = "Str0ng!Passw0rd";
    private const string DeviceId = "device-1";
    private const string ClientIp = "127.0.0.1";

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeAccessTokenIssuer _accessTokenIssuer = new();
    private readonly AuthSettings _settings = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();

    private LoginCommandHandler LoginHandler =>
        new(
            _users,
            _passwordHasher,
            _refreshTokens,
            _unitOfWork,
            _settings,
            _timeProvider,
            new TokenPairFactory(_accessTokenIssuer, _settings, _timeProvider),
            new LoginCommandValidator(),
            new InMemoryLoginAttemptThrottler(_settings),
            new AuditLogWriter(_auditEntries, new FakeAuditContextProvider()));

    private RefreshCommandHandler RefreshHandler =>
        new(
            _users,
            _refreshTokens,
            _unitOfWork,
            _timeProvider,
            new TokenPairFactory(_accessTokenIssuer, _settings, _timeProvider),
            new RefreshCommandValidator());

    private LogoutCommandHandler LogoutHandler =>
        new(_refreshTokens, _unitOfWork, _timeProvider, new LogoutCommandValidator());

    private LogoutAllCommandHandler LogoutAllHandler =>
        new(_refreshTokens, _unitOfWork, _timeProvider);

    [Fact]
    public async Task Login_With_Valid_Credentials_Issues_Token_Pair()
    {
        var customer = CreateVerifiedCustomer();

        var result = await LoginHandler.Handle(
            new LoginCommand(customer.Email, Password, DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var pair = result.Value;
        Assert.False(string.IsNullOrWhiteSpace(pair.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(pair.RefreshToken));
        Assert.Equal(customer.Id, pair.UserId);
        Assert.Equal(customer.Email, pair.Email);
        Assert.Equal([IdentityRoles.Customer], pair.Roles);
        Assert.InRange(pair.ExpiresInSeconds, 899, 900);
        Assert.Equal(0, customer.FailedLoginCount);
        Assert.Null(customer.LockoutEndAtUtc);

        var stored = Assert.Single(_refreshTokens.Tokens);
        Assert.Equal(customer.Id, stored.UserId);
        Assert.NotEqual(pair.RefreshToken, stored.TokenHash);
        Assert.Equal(RefreshTokens.Hash(pair.RefreshToken), stored.TokenHash);
        Assert.Equal(DeviceId, stored.DeviceId);
        Assert.True(stored.CanBeUsed(DateTime.UtcNow));
        Assert.Equal(1, _accessTokenIssuer.IssueCount);
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Writes_Audit_Entry()
    {
        var customer = CreateVerifiedCustomer();

        var result = await LoginHandler.Handle(
            new LoginCommand(customer.Email, Password, DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(_auditEntries.Entries);
        Assert.Equal(AuditActions.Login, entry.Action);
        Assert.Equal("Customer", entry.EntityType);
        Assert.Equal(customer.Id.ToString(), entry.EntityId);
        Assert.Equal(customer.Id, entry.ActorId);
        Assert.Equal(AuditActorType.User, entry.ActorType);
        Assert.Null(entry.PreviousHash);
    }

    [Fact]
    public async Task Login_Failure_Does_Not_Write_Audit_Entry()
    {
        var result = await LoginHandler.Handle(
            new LoginCommand("nobody@example.com", "Wrong!Passw0rd", DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_auditEntries.Entries);
    }

    [Fact]
    public async Task Login_Wrong_Password_Returns_InvalidCredentials_And_Increments_Counter()
    {
        var customer = CreateVerifiedCustomer();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var result = await LoginHandler.Handle(
                new LoginCommand(customer.Email, "Wrong!Passw0rd", DeviceId, ClientIp),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
            Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
            Assert.Equal(attempt, customer.FailedLoginCount);
            Assert.Null(customer.LockoutEndAtUtc);
        }

        Assert.Empty(_refreshTokens.Tokens);
    }

    [Fact]
    public async Task Login_After_Max_Failed_Attempts_Locks_Account_With_RetryAfter()
    {
        var customer = CreateVerifiedCustomer();

        Result<LoginResult> result = null!;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            result = await LoginHandler.Handle(
                new LoginCommand(customer.Email, "Wrong!Passw0rd", DeviceId, ClientIp),
                CancellationToken.None);
        }

        Assert.True(result.IsFailure);
        Assert.Equal("ERR_AUTH_003", result.Error.Code);
        Assert.Equal(ErrorType.Locked, result.Error.Type);
        Assert.NotNull(result.Error.RetryAfterSeconds);
        Assert.InRange(result.Error.RetryAfterSeconds!.Value, 850, 900);
        Assert.True(customer.IsLockedOut(DateTime.UtcNow));
        Assert.Equal(0, customer.FailedLoginCount);

        var afterLockout = await LoginHandler.Handle(
            new LoginCommand(customer.Email, Password, DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(afterLockout.IsFailure);
        Assert.Equal(ErrorType.Locked, afterLockout.Error.Type);
    }

    [Fact]
    public async Task Login_Unverified_Email_Returns_Forbidden()
    {
        var customer = CreateVerifiedCustomer(verified: false);

        var result = await LoginHandler.Handle(
            new LoginCommand(customer.Email, Password, DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.EmailNotVerified, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task Login_Unknown_Email_Returns_InvalidCredentials()
    {
        var result = await LoginHandler.Handle(
            new LoginCommand("nobody@example.com", Password, DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Login_Too_Many_Failures_From_Same_Ip_Returns_TooManyAttempts_With_RetryAfter()
    {
        CreateVerifiedCustomer();
        var settings = new AuthSettings { MaxFailedLoginAttemptsPerIp = 3, LoginAttemptWindowMinutes = 5 };
        var throttler = new InMemoryLoginAttemptThrottler(settings);
        var handler = new LoginCommandHandler(
            _users,
            _passwordHasher,
            _refreshTokens,
            _unitOfWork,
            settings,
            _timeProvider,
            new TokenPairFactory(_accessTokenIssuer, settings, _timeProvider),
            new LoginCommandValidator(),
            throttler,
            new AuditLogWriter(_auditEntries, new FakeAuditContextProvider()));

        Result<LoginResult> result = null!;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            result = await handler.Handle(
                new LoginCommand("spray@example.com", "Wrong!Passw0rd", DeviceId, "198.51.100.10"),
                CancellationToken.None);
        }

        Assert.True(result.IsFailure);
        Assert.Equal("ERR_AUTH_005", result.Error.Code);
        Assert.Equal(ErrorType.TooManyRequests, result.Error.Type);
        Assert.NotNull(result.Error.RetryAfterSeconds);
        Assert.True(result.Error.RetryAfterSeconds!.Value > 0);
        Assert.True(throttler.IsBlocked("198.51.100.10", DateTime.UtcNow, out _));

        var stillBlocked = await handler.Handle(
            new LoginCommand("spray@example.com", Password, DeviceId, "198.51.100.10"),
            CancellationToken.None);

        Assert.True(stillBlocked.IsFailure);
        Assert.Equal(ErrorType.TooManyRequests, stillBlocked.Error.Type);
    }

    [Fact]
    public async Task Login_Success_Resets_Ip_Failure_Counter()
    {
        var customer = CreateVerifiedCustomer();
        var settings = new AuthSettings { MaxFailedLoginAttemptsPerIp = 3, LoginAttemptWindowMinutes = 5 };
        var throttler = new InMemoryLoginAttemptThrottler(settings);
        var handler = new LoginCommandHandler(
            _users,
            _passwordHasher,
            _refreshTokens,
            _unitOfWork,
            settings,
            _timeProvider,
            new TokenPairFactory(_accessTokenIssuer, settings, _timeProvider),
            new LoginCommandValidator(),
            throttler,
            new AuditLogWriter(_auditEntries, new FakeAuditContextProvider()));

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var failed = await handler.Handle(
                new LoginCommand(customer.Email, "Wrong!Passw0rd", DeviceId, "203.0.113.5"),
                CancellationToken.None);
            Assert.True(failed.IsFailure);
        }

        var success = await handler.Handle(
            new LoginCommand(customer.Email, Password, DeviceId, "203.0.113.5"),
            CancellationToken.None);
        Assert.True(success.IsSuccess);

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var failed = await handler.Handle(
                new LoginCommand(customer.Email, "Wrong!Passw0rd", DeviceId, "203.0.113.5"),
                CancellationToken.None);
            Assert.True(failed.IsFailure);
            Assert.Equal(ErrorType.Unauthorized, failed.Error.Type);
        }
    }

    [Fact]
    public async Task Login_Failures_From_Different_Ips_Do_Not_Share_Counter()
    {
        var settings = new AuthSettings { MaxFailedLoginAttemptsPerIp = 3, LoginAttemptWindowMinutes = 5 };
        var throttler = new InMemoryLoginAttemptThrottler(settings);
        var handler = new LoginCommandHandler(
            _users,
            _passwordHasher,
            _refreshTokens,
            _unitOfWork,
            settings,
            _timeProvider,
            new TokenPairFactory(_accessTokenIssuer, settings, _timeProvider),
            new LoginCommandValidator(),
            throttler,
            new AuditLogWriter(_auditEntries, new FakeAuditContextProvider()));

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var result = await handler.Handle(
                new LoginCommand("spray@example.com", "Wrong!Passw0rd", DeviceId, $"198.51.100.{attempt}"),
                CancellationToken.None);
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        }
    }

    [Fact]
    public async Task Refresh_With_Valid_Token_Rotates_And_Revokes_Old()
    {
        var (login, customer) = await LoginAsync();

        var result = await RefreshHandler.Handle(
            new RefreshCommand(login.RefreshToken),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var oldToken = _refreshTokens.Tokens.Single(token => token.TokenHash == RefreshTokens.Hash(login.RefreshToken));
        var newToken = _refreshTokens.Tokens.Last();
        Assert.True(oldToken.IsRevoked);
        Assert.Equal(newToken.Id, oldToken.ReplacedById);
        Assert.Equal(oldToken.FamilyId, newToken.FamilyId);
        Assert.Equal(DeviceId, newToken.DeviceId);
        Assert.Equal(customer.Id, result.Value.UserId);
        Assert.NotEqual(login.AccessToken, result.Value.AccessToken);
        Assert.NotEqual(login.RefreshToken, result.Value.RefreshToken);
    }

    [Fact]
    public async Task Refresh_With_Sequential_Reuse_Returns_Invalid_And_Does_Not_Rotate()
    {
        var (login, _) = await LoginAsync();

        var first = await RefreshHandler.Handle(new RefreshCommand(login.RefreshToken), CancellationToken.None);
        var second = await RefreshHandler.Handle(new RefreshCommand(login.RefreshToken), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(AuthErrors.RefreshTokenInvalid, second.Error);

        var tokens = _refreshTokens.Tokens.ToList();
        Assert.Equal(2, tokens.Count);
        Assert.Equal(1, tokens.Count(token => token.IsRevoked));
        Assert.Equal(1, tokens.Count(token => token.CanBeUsed(DateTime.UtcNow)));
    }

    [Fact]
    public async Task Refresh_Token_Is_Single_Use_Under_Concurrent_Consumption()
    {
        await LoginAsync();
        var token = _refreshTokens.Tokens.Single();

        var first = await _refreshTokens.TryRevokeAsync(token.Id, DateTime.UtcNow, CancellationToken.None);
        var second = await _refreshTokens.TryRevokeAsync(token.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
    }

    [Fact]
    public async Task Refresh_With_Unknown_Token_Returns_Invalid()
    {
        var result = await RefreshHandler.Handle(
            new RefreshCommand("r_never-issued"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.RefreshTokenInvalid, result.Error);
    }

    [Fact]
    public async Task Refresh_With_Expired_Token_Returns_Invalid()
    {
        var customer = CreateVerifiedCustomer();
        var token = RefreshToken.Create(
            customer.Id,
            Guid.NewGuid(),
            DeviceId,
            RefreshTokens.Hash("r_expired-token"),
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow);
        _refreshTokens.Add(token);

        var result = await RefreshHandler.Handle(
            new RefreshCommand("r_expired-token"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.RefreshTokenInvalid, result.Error);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public async Task Logout_Revokes_Device_Token()
    {
        var (login, _) = await LoginAsync();

        var result = await LogoutHandler.Handle(
            new LogoutCommand(login.RefreshToken),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var token = _refreshTokens.Tokens.Single(candidate => candidate.TokenHash == RefreshTokens.Hash(login.RefreshToken));
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public async Task Logout_Unknown_Token_Is_Idempotent()
    {
        var result = await LogoutHandler.Handle(
            new LogoutCommand("r_never-issued"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task LogoutAll_Revokes_All_User_Tokens()
    {
        var (login, customer) = await LoginAsync();
        var secondLogin = await LoginHandler.Handle(
            new LoginCommand(customer.Email, Password, "device-2", ClientIp),
            CancellationToken.None);

        var result = await LogoutAllHandler.Handle(
            new LogoutAllCommand(customer.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(_refreshTokens.Tokens.All(token => token.IsRevoked));
        Assert.NotEqual(login.RefreshToken, secondLogin.Value.RefreshToken);
    }

    private Customer CreateVerifiedCustomer(string email = "ahmed@example.com", bool verified = true)
    {
        var rawToken = "verify-token";
        var customer = Customer.Register(
            email,
            "Ahmed Hassan",
            "ar",
            "AED",
            _passwordHasher.Hash(Password),
            VerificationTokens.Hash(rawToken),
            DateTime.UtcNow.AddHours(24),
            rawToken);

        if (verified)
        {
            customer.VerifyEmail(VerificationTokens.Hash(rawToken), DateTime.UtcNow);
        }

        _users.Customers.Add(customer);
        _users.ExistingByEmail = customer;
        return customer;
    }

    private async Task<(LoginResult Result, Customer Customer)> LoginAsync()
    {
        var customer = CreateVerifiedCustomer();
        var result = await LoginHandler.Handle(
            new LoginCommand(customer.Email, Password, DeviceId, ClientIp),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        return (result.Value, customer);
    }
}

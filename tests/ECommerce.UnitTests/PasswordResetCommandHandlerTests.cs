using ECommerce.Domain.Events;
using ECommerce.Domain.Identity;
using ECommerce.UseCases.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Handlers;

namespace ECommerce.UnitTests;

public sealed class PasswordResetCommandHandlerTests
{
    private const string Password = "Str0ng!Passw0rd";
    private const string NewPassword = "N3w!Passw0rd2026";

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private ForgotPasswordCommandHandler ForgotPasswordHandler =>
        new(_users, _unitOfWork, _timeProvider, new ForgotPasswordCommandValidator());

    private ResetPasswordCommandHandler ResetPasswordHandler(bool breached = false) =>
        new(
            _users,
            _passwordHasher,
            new FakeBreachChecker(breached),
            _refreshTokens,
            _unitOfWork,
            _timeProvider,
            new ResetPasswordCommandValidator());

    [Fact]
    public async Task ForgotPassword_With_Known_Email_Issues_Token_And_Event()
    {
        var customer = CreateVerifiedCustomer();

        var result = await ForgotPasswordHandler.Handle(
            new ForgotPasswordCommand(customer.Email),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(customer.PasswordResetTokenHash);
        Assert.NotNull(customer.PasswordResetTokenExpiresAt);
        Assert.True(customer.PasswordResetTokenExpiresAt > DateTime.UtcNow.AddMinutes(29));

        var domainEvent = Assert.Single(customer.DomainEvents.OfType<PasswordResetRequested>());
        Assert.Equal(customer.Id, domainEvent.CustomerId);
        Assert.Equal(customer.Email, domainEvent.Email);
        Assert.Equal(RefreshTokens.Hash(domainEvent.ResetToken), customer.PasswordResetTokenHash);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ForgotPassword_With_Unknown_Email_Returns_Success_Without_Side_Effects()
    {
        var result = await ForgotPasswordHandler.Handle(
            new ForgotPasswordCommand("nobody@example.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_users.Customers);
    }

    [Fact]
    public async Task ForgotPassword_With_Invalid_Email_Returns_Validation_Failure()
    {
        var result = await ForgotPasswordHandler.Handle(
            new ForgotPasswordCommand("not-an-email"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ForgotPassword_Second_Request_Invalidates_Previous_Token()
    {
        var customer = CreateVerifiedCustomer();
        var firstRaw = IssueResetToken(customer);

        var result = await ForgotPasswordHandler.Handle(
            new ForgotPasswordCommand(customer.Email),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var firstReset = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand(firstRaw, NewPassword),
            CancellationToken.None);
        Assert.True(firstReset.IsFailure);
        Assert.Equal(CustomerErrors.PasswordResetTokenInvalid, firstReset.Error);
    }

    [Fact]
    public async Task ResetPassword_With_Valid_Token_Succeeds_And_Revokes_Sessions()
    {
        var customer = CreateVerifiedCustomer();
        var rawToken = IssueResetToken(customer);
        var existingSession = RefreshToken.Create(
            customer.Id,
            Guid.NewGuid(),
            "device-1",
            "stored-hash",
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow);
        _refreshTokens.Add(existingSession);

        var result = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand(rawToken, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"hash:{NewPassword}", customer.PasswordHash);
        Assert.Null(customer.PasswordResetTokenHash);
        Assert.Null(customer.PasswordResetTokenExpiresAt);
        Assert.True(existingSession.IsRevoked);

        Assert.False(customer.EmailVerified);
        Assert.Null(customer.EmailVerifiedAt);
        Assert.NotNull(customer.VerificationTokenHash);
        Assert.NotNull(customer.VerificationTokenExpiresAt);

        var completed = Assert.Single(customer.DomainEvents.OfType<PasswordReset>());
        Assert.NotNull(completed.NewVerificationToken);
        Assert.Equal(customer.VerificationTokenHash, RefreshTokens.Hash(completed.NewVerificationToken));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ResetPassword_With_Invalid_Token_Returns_Invalid()
    {
        var customer = CreateVerifiedCustomer();
        IssueResetToken(customer);

        var result = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand("r_bogus-token", NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.PasswordResetTokenInvalid, result.Error);
        Assert.Equal("hash:" + Password, customer.PasswordHash);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task ResetPassword_With_Expired_Token_Returns_Expired()
    {
        var customer = CreateVerifiedCustomer();
        var rawToken = "expired-reset-token";
        var utcNow = DateTime.UtcNow;
        customer.IssuePasswordResetToken(
            RefreshTokens.Hash(rawToken),
            rawToken,
            utcNow.AddMinutes(-1),
            utcNow);

        var result = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand(rawToken, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.PasswordResetTokenExpired, result.Error);
    }

    [Fact]
    public async Task ResetPassword_With_Breached_Password_Returns_Breached()
    {
        var customer = CreateVerifiedCustomer();
        var rawToken = IssueResetToken(customer);

        var result = await ResetPasswordHandler(breached: true).Handle(
            new ResetPasswordCommand(rawToken, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.BreachedPassword, result.Error);
        Assert.Equal("hash:" + Password, customer.PasswordHash);
        Assert.NotNull(customer.PasswordResetTokenHash);
    }

    [Fact]
    public async Task ResetPassword_Token_Is_Single_Use()
    {
        var customer = CreateVerifiedCustomer();
        var rawToken = IssueResetToken(customer);

        var first = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand(rawToken, NewPassword),
            CancellationToken.None);
        var second = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand(rawToken, "An0ther!Passw0rd"),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(CustomerErrors.PasswordResetTokenInvalid, second.Error);
        Assert.Equal($"hash:{NewPassword}", customer.PasswordHash);
    }

    [Fact]
    public async Task ResetPassword_Unlocks_Locked_Account()
    {
        var customer = CreateVerifiedCustomer();
        customer.RecordFailedLogin(1, TimeSpan.FromMinutes(15), DateTime.UtcNow);
        customer.RecordFailedLogin(1, TimeSpan.FromMinutes(15), DateTime.UtcNow);
        customer.RecordFailedLogin(1, TimeSpan.FromMinutes(15), DateTime.UtcNow);
        customer.RecordFailedLogin(1, TimeSpan.FromMinutes(15), DateTime.UtcNow);
        customer.RecordFailedLogin(1, TimeSpan.FromMinutes(15), DateTime.UtcNow);
        Assert.True(customer.IsLockedOut(DateTime.UtcNow));

        var rawToken = IssueResetToken(customer);
        var result = await ResetPasswordHandler().Handle(
            new ResetPasswordCommand(rawToken, NewPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, customer.FailedLoginCount);
        Assert.Null(customer.LockoutEndAtUtc);
        Assert.False(customer.IsLockedOut(DateTime.UtcNow));
    }

    private Customer CreateVerifiedCustomer(string email = "ahmed@example.com")
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
        customer.VerifyEmail(VerificationTokens.Hash(rawToken), DateTime.UtcNow);

        _users.Customers.Add(customer);
        _users.ExistingByEmail = customer;
        return customer;
    }

    private string IssueResetToken(Customer customer)
    {
        var rawToken = $"reset-{Guid.NewGuid():N}";
        customer.IssuePasswordResetToken(
            RefreshTokens.Hash(rawToken),
            rawToken,
            DateTime.UtcNow.AddMinutes(30),
            DateTime.UtcNow);
        return rawToken;
    }
}

using ECommerce.Domain.Events;
using ECommerce.Domain.Identity;

namespace ECommerce.UnitTests;

public sealed class CustomerTests
{
    private const string ValidEmail = "ahmed@example.com";
    private const string PasswordHash = "$2a$12$hash";
    private const string RawToken = "A1B2C3D4";
    private const string TokenHash = "TOKENHASH";

    [Fact]
    public void Register_Sets_State_And_Raises_CustomerRegistered()
    {
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var customer = Customer.Register(
            ValidEmail,
            "Ahmed Hassan",
            "ar",
            "AED",
            PasswordHash,
            TokenHash,
            expiresAt,
            RawToken);

        Assert.Equal(ValidEmail, customer.Email);
        Assert.Equal("Ahmed Hassan", customer.DisplayName);
        Assert.Equal("ar", customer.Locale);
        Assert.Equal("AED", customer.Currency);
        Assert.Equal(PasswordHash, customer.PasswordHash);
        Assert.False(customer.EmailVerified);
        Assert.Equal(TokenHash, customer.VerificationTokenHash);
        Assert.Equal(expiresAt, customer.VerificationTokenExpiresAt);

        var domainEvent = Assert.Single(customer.DomainEvents);
        var registered = Assert.IsType<CustomerRegistered>(domainEvent);
        Assert.Equal(customer.Id, registered.CustomerId);
        Assert.Equal(ValidEmail, registered.Email);
        Assert.Equal(RawToken, registered.VerificationToken);
        Assert.Equal(expiresAt, registered.ExpiresAtUtc);
    }

    [Fact]
    public void VerifyEmail_With_Valid_Token_Succeeds_And_Clears_Token()
    {
        var customer = CreateCustomer(expiresAt: DateTime.UtcNow.AddHours(1));

        var result = customer.VerifyEmail(TokenHash, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(customer.EmailVerified);
        Assert.NotNull(customer.EmailVerifiedAt);
        Assert.Null(customer.VerificationTokenHash);
        Assert.Null(customer.VerificationTokenExpiresAt);
    }

    [Fact]
    public void VerifyEmail_With_Wrong_Token_Fails()
    {
        var customer = CreateCustomer(expiresAt: DateTime.UtcNow.AddHours(1));

        var result = customer.VerifyEmail("WRONG", DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.VerificationTokenInvalid, result.Error);
        Assert.False(customer.EmailVerified);
    }

    [Fact]
    public void VerifyEmail_With_Expired_Token_Fails()
    {
        var customer = CreateCustomer(expiresAt: DateTime.UtcNow.AddHours(-1));

        var result = customer.VerifyEmail(TokenHash, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.VerificationTokenExpired, result.Error);
        Assert.False(customer.EmailVerified);
    }

    [Fact]
    public void VerifyEmail_Is_Single_Use()
    {
        var customer = CreateCustomer(expiresAt: DateTime.UtcNow.AddHours(1));
        var now = DateTime.UtcNow;

        var first = customer.VerifyEmail(TokenHash, now);
        var second = customer.VerifyEmail(TokenHash, now);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(CustomerErrors.AlreadyVerified, second.Error);
        Assert.True(customer.EmailVerified);
    }

    private static Customer CreateCustomer(DateTime expiresAt) =>
        Customer.Register(ValidEmail, "Ahmed Hassan", "ar", "AED", PasswordHash, TokenHash, expiresAt, RawToken);
}

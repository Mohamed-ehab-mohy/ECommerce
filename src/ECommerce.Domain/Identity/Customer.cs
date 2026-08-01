using System.Security.Cryptography;
using ECommerce.Domain.Common;
using ECommerce.Domain.Events;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Identity;

public sealed class Customer : BaseEntity<Guid>
{
    private Customer()
    {
        Email = string.Empty;
        DisplayName = string.Empty;
        Locale = string.Empty;
        Currency = string.Empty;
        PasswordHash = string.Empty;
    }

    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    public string Locale { get; private set; }

    public string Currency { get; private set; }

    public string PasswordHash { get; private set; }

    public bool EmailVerified { get; private set; }

    public DateTime? EmailVerifiedAt { get; private set; }

    public string? VerificationTokenHash { get; private set; }

    public DateTime? VerificationTokenExpiresAt { get; private set; }

    public static Customer Register(
        string email,
        string displayName,
        string locale,
        string currency,
        string passwordHash,
        string verificationTokenHash,
        DateTime verificationTokenExpiresAtUtc,
        string verificationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Locale = locale,
            Currency = currency,
            PasswordHash = passwordHash
        };

        customer.IssueVerificationToken(verificationTokenHash, verificationTokenExpiresAtUtc);
        customer.AddDomainEvent(new CustomerRegistered(
            customer.Id,
            customer.Email,
            customer.DisplayName,
            customer.Locale,
            customer.Currency,
            verificationToken,
            verificationTokenExpiresAtUtc));

        return customer;
    }

    public void IssueVerificationToken(string tokenHash, DateTime expiresAtUtc)
    {
        VerificationTokenHash = tokenHash;
        VerificationTokenExpiresAt = expiresAtUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result VerifyEmail(string tokenHash, DateTime utcNow)
    {
        if (EmailVerified)
        {
            return Result.Failure(CustomerErrors.AlreadyVerified);
        }

        if (VerificationTokenHash is null ||
            VerificationTokenExpiresAt is null ||
            !TokensMatch(VerificationTokenHash, tokenHash))
        {
            return Result.Failure(CustomerErrors.VerificationTokenInvalid);
        }

        if (VerificationTokenExpiresAt < utcNow)
        {
            return Result.Failure(CustomerErrors.VerificationTokenExpired);
        }

        EmailVerified = true;
        EmailVerifiedAt = utcNow;
        VerificationTokenHash = null;
        VerificationTokenExpiresAt = null;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    private static bool TokensMatch(string storedHash, string providedHash) =>
        CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(storedHash),
            System.Text.Encoding.UTF8.GetBytes(providedHash));
}

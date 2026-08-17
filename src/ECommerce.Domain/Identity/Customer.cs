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

    public string? Phone { get; private set; }

    public string Locale { get; private set; }

    public string Currency { get; private set; }

    public string PasswordHash { get; private set; }

    public bool EmailVerified { get; private set; }

    public DateTime? EmailVerifiedAt { get; private set; }

    public string? VerificationTokenHash { get; private set; }

    public DateTime? VerificationTokenExpiresAt { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutEndAtUtc { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }

    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

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
            PasswordHash = passwordHash,
            FailedLoginCount = 0
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

    public void UpdateProfile(string? displayName, string? phone, string? locale, string? currency, DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = displayName;
        }

        if (phone is not null)
        {
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        }

        if (!string.IsNullOrWhiteSpace(locale))
        {
            Locale = locale;
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            Currency = currency;
        }

        UpdatedAt = utcNow;

        AddDomainEvent(new ProfileUpdated(Id, DisplayName, Phone, Locale, Currency));
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

    public bool IsLockedOut(DateTime utcNow) => LockoutEndAtUtc is { } end && end > utcNow;

    public TimeSpan RemainingLockout(DateTime utcNow) =>
        LockoutEndAtUtc is { } end && end > utcNow ? end - utcNow : TimeSpan.Zero;

    public void RecordFailedLogin(int maxAttempts, TimeSpan lockoutDuration, DateTime utcNow)
    {
        FailedLoginCount++;

        if (FailedLoginCount >= maxAttempts)
        {
            LockoutEndAtUtc = utcNow.Add(lockoutDuration);
            FailedLoginCount = 0;
        }

        UpdatedAt = utcNow;
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedLoginCount = 0;
        LockoutEndAtUtc = null;
        UpdatedAt = utcNow;
    }

    public void IssuePasswordResetToken(string tokenHash, string resetToken, DateTime expiresAtUtc, DateTime utcNow)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAt = expiresAtUtc;
        UpdatedAt = utcNow;

        AddDomainEvent(new PasswordResetRequested(
            Id,
            Email,
            DisplayName,
            resetToken,
            expiresAtUtc));
    }

    public Result ResetPassword(
        string newPasswordHash,
        string resetTokenHash,
        string reVerifyToken,
        string reVerifyTokenHash,
        DateTime reVerifyTokenExpiresAtUtc,
        DateTime utcNow)
    {
        if (PasswordResetTokenHash is null || !TokensMatch(PasswordResetTokenHash, resetTokenHash))
        {
            return Result.Failure(CustomerErrors.PasswordResetTokenInvalid);
        }

        if (PasswordResetTokenExpiresAt is null || PasswordResetTokenExpiresAt < utcNow)
        {
            return Result.Failure(CustomerErrors.PasswordResetTokenExpired);
        }

        PasswordHash = newPasswordHash;
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
        FailedLoginCount = 0;
        LockoutEndAtUtc = null;

        EmailVerified = false;
        EmailVerifiedAt = null;
        VerificationTokenHash = reVerifyTokenHash;
        VerificationTokenExpiresAt = reVerifyTokenExpiresAtUtc;
        UpdatedAt = utcNow;

        AddDomainEvent(new PasswordReset(
            Id,
            Email,
            DisplayName,
            reVerifyToken,
            reVerifyTokenExpiresAtUtc));

        return Result.Success();
    }

    public void Close(DateTime utcNow)
    {
        IsDeleted = true;
        UpdatedAt = utcNow;

        AddDomainEvent(new AccountClosed(Id));
    }

    public void Anonymize(DateTime utcNow)
    {
        var anonymousId = Id.ToString()[..8];
        Email = $"deleted-{anonymousId}@anonymized.invalid";
        DisplayName = "Deleted User";
        Phone = null;
        Locale = "en";
        Currency = "USD";
        PasswordHash = string.Empty;
        EmailVerified = false;
        EmailVerifiedAt = null;
        VerificationTokenHash = null;
        VerificationTokenExpiresAt = null;
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
        FailedLoginCount = 0;
        LockoutEndAtUtc = null;
        IsDeleted = true;
        UpdatedAt = utcNow;

        AddDomainEvent(new AccountErased(Id));
    }
}

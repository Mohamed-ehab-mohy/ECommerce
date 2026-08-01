using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Identity;

public static class CustomerErrors
{
    public static readonly Error EmailAlreadyExists = new(
        "Customer.EmailAlreadyExists",
        "An account with this email already exists.",
        ErrorType.Conflict);

    public static readonly Error WeakPassword = new(
        "Customer.WeakPassword",
        "The password does not meet the policy requirements.",
        ErrorType.Validation);

    public static readonly Error BreachedPassword = new(
        "Customer.BreachedPassword",
        "This password is known in a data breach; choose a different one.",
        ErrorType.Validation);

    public static readonly Error VerificationTokenInvalid = new(
        "Customer.VerificationTokenInvalid",
        "The verification token is invalid or has already been used.",
        ErrorType.Validation);

    public static readonly Error VerificationTokenExpired = new(
        "Customer.VerificationTokenExpired",
        "The verification token has expired.",
        ErrorType.Validation);

    public static readonly Error AlreadyVerified = new(
        "Customer.AlreadyVerified",
        "The email address is already verified.",
        ErrorType.Validation);

    public static readonly Error EmailNotVerified = new(
        "Customer.EmailNotVerified",
        "Email not verified; verify your email address before signing in.",
        ErrorType.Forbidden);
}

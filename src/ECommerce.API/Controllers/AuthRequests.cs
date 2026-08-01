namespace ECommerce.API.Controllers;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string Locale, string Currency);

public sealed record VerifyEmailRequest(string Token);

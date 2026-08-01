namespace ECommerce.API.Controllers;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string Locale, string Currency);

public sealed record VerifyEmailRequest(string Token);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

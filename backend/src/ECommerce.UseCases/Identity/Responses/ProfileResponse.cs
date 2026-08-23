namespace ECommerce.UseCases.Identity.Responses;

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    string Locale,
    string Currency,
    bool EmailVerified);

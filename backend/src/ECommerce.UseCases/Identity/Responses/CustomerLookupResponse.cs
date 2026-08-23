namespace ECommerce.UseCases.Identity.Responses;

public sealed record CustomerLookupResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? Phone,
    string Locale,
    string Currency,
    bool EmailVerified,
    DateTime CreatedAt);

public sealed record PagedCustomersResponse(
    IReadOnlyList<CustomerLookupResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

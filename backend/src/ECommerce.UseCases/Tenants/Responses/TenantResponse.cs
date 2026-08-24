namespace ECommerce.UseCases.Tenants.Responses;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    string Subdomain,
    string? CustomDomain,
    string Status);

namespace ECommerce.Infrastructure.ReadModels;

public sealed record ProductSummaryReadModel(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    decimal ListPrice,
    bool IsActive);

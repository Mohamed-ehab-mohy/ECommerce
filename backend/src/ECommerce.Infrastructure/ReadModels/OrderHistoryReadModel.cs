namespace ECommerce.Infrastructure.ReadModels;

public sealed record OrderHistoryReadModel(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal GrandTotal,
    string Currency,
    DateTime? PlacedAt,
    DateTime CreatedAt);

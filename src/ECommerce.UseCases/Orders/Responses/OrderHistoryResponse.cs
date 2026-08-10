namespace ECommerce.UseCases.Orders.Responses;

public sealed record OrderHistoryItemResponse(
    Guid OrderId,
    string OrderNumber,
    string Status,
    decimal GrandTotal,
    string Currency,
    DateTime? PlacedAt,
    int LineCount);

public sealed record OrderHistoryResponse(
    IReadOnlyList<OrderHistoryItemResponse> Items,
    string? NextCursor,
    bool HasNext,
    int PageSize);

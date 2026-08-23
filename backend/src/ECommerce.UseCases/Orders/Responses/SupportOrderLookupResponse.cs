namespace ECommerce.UseCases.Orders.Responses;

public sealed record SupportOrderItemResponse(
    Guid OrderId,
    string OrderNumber,
    Guid? CustomerId,
    string MaskedEmail,
    string Status,
    decimal GrandTotal,
    string Currency,
    DateTime? PlacedAt);

public sealed record SupportOrderLookupResponse(IReadOnlyList<SupportOrderItemResponse> Orders);

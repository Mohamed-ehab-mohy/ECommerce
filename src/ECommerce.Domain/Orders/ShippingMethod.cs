namespace ECommerce.Domain.Orders;

public sealed record ShippingMethod(
    string Id,
    string Name,
    decimal Rate,
    string Currency,
    string? EstimatedDays);

namespace ECommerce.UseCases.Grpc.Ports;

public sealed record OrderStatusDto(
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string CustomerEmail,
    DateTime? PlacedAt,
    IReadOnlyList<OrderTimelineEntryDto> Timeline);

public sealed record OrderTimelineEntryDto(string Status, string Note, DateTime OccurredAt);

public sealed record ProductSummaryDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    decimal ListPrice,
    bool IsActive);

public interface IGrpcQueryService
{
    Task<OrderStatusDto?> GetOrderStatusAsync(string orderNumber, CancellationToken cancellationToken);
    Task<ProductSummaryDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken);
}

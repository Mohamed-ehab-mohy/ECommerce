using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record OrderBackordered(
    Guid OrderId,
    string OrderNumber,
    string CustomerEmail,
    IReadOnlyList<BackorderLine> Lines) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record BackorderLine(Guid ProductId, string Sku, int Quantity);

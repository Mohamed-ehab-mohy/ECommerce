using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record BackorderFilled(
    Guid OrderId,
    string OrderNumber,
    string CustomerEmail,
    Guid ProductId,
    string Sku,
    int Quantity) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

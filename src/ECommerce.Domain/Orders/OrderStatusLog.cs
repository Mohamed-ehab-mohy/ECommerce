namespace ECommerce.Domain.Orders;

public sealed class OrderStatusLog
{
    private OrderStatusLog()
    {
        ActorType = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; internal set; }

    public OrderStatus? FromStatus { get; private set; }

    public OrderStatus ToStatus { get; private set; }

    public string ActorType { get; private set; }

    public Guid? ActorId { get; private set; }

    public string? TraceId { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public static OrderStatusLog Create(
        Guid orderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string actorType,
        Guid? actorId,
        string? traceId,
        DateTime occurredAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorType = actorType,
            ActorId = actorId,
            TraceId = traceId,
            OccurredAt = occurredAt
        };
}

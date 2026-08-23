using ECommerce.Domain.Common;

namespace ECommerce.Domain.Orders;

public sealed class IdempotencyKey : BaseEntity<Guid>
{
    private IdempotencyKey()
    {
        Key = string.Empty;
    }

    public string Key { get; private set; }

    public Guid CheckoutId { get; private set; }

    public Guid OrderId { get; private set; }

    public static IdempotencyKey Create(string key, Guid checkoutId, Guid orderId, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            CheckoutId = checkoutId,
            OrderId = orderId,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
}

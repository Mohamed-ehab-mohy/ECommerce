using ECommerce.Domain.Common;

namespace ECommerce.Domain.Orders;

public sealed class ReturnRequest : BaseEntity<Guid>
{
    private readonly List<ReturnRequestItem> _items = [];

    private ReturnRequest() { Reason = string.Empty; Currency = string.Empty; }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Reason { get; private set; }
    public string Currency { get; private set; }
    public decimal RefundAmount { get; private set; }
    public bool Restock { get; private set; }
    public ReturnRequestStatus Status { get; private set; }
    public string? AdminNotes { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public IReadOnlyCollection<ReturnRequestItem> Items => _items;

    public static ReturnRequest Create(Guid orderId, Guid customerId, string reason, string currency,
        decimal refundAmount, bool restock, IReadOnlyCollection<ReturnRequestItem> items, DateTime utcNow)
    {
        var request = new ReturnRequest
        {
            Id = Guid.NewGuid(), OrderId = orderId, CustomerId = customerId,
            Reason = reason.Trim(), Currency = currency, RefundAmount = refundAmount,
            Restock = restock, Status = ReturnRequestStatus.Requested,
            CreatedAt = utcNow, UpdatedAt = utcNow
        };
        request._items.AddRange(items);
        return request;
    }

    public void Approve(Guid reviewedBy, DateTime utcNow)
    {
        Status = ReturnRequestStatus.Approved;
        ReviewedBy = reviewedBy;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Reject(Guid reviewedBy, string? notes, DateTime utcNow)
    {
        Status = ReturnRequestStatus.Rejected;
        ReviewedBy = reviewedBy;
        AdminNotes = notes;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Complete(DateTime utcNow)
    {
        Status = ReturnRequestStatus.Completed;
        UpdatedAt = utcNow;
    }
}

namespace ECommerce.Domain.Payments;

/// <summary>Refund workflow state machine (FRS-I-004): Requested → Approved → Executing → Completed | Failed.</summary>
public enum RefundStatus
{
    Requested,
    Approved,
    Rejected,
    Executing,
    Completed,
    Failed
}

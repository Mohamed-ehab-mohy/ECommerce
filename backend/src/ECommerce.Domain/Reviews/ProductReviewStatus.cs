namespace ECommerce.Domain.Reviews;

/// <summary>Lifecycle of a product review (FRS-K-001/002/004).</summary>
public enum ProductReviewStatus
{
    Pending = 0,
    Published = 1,
    Rejected = 2,
    Removed = 3
}

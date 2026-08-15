namespace ECommerce.UseCases.Reviews.Ports;

/// <summary>Confirms a customer purchased a product before they can review it (FRS-K-001).</summary>
public interface IVerifiedPurchaseChecker
{
    Task<bool> HasPurchasedAsync(Guid customerId, Guid productId, CancellationToken cancellationToken);
}

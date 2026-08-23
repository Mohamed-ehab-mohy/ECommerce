namespace ECommerce.Domain.Wallets;

public enum LoyaltyTransactionType
{
    Earned,
    Redeemed,
    Expired
}

public sealed class LoyaltyTransaction
{
    private LoyaltyTransaction() { } // EF Core

    public LoyaltyTransaction(
        Guid id, 
        Guid walletId, 
        int points, 
        LoyaltyTransactionType type, 
        string referenceId, 
        int pointsAfter)
    {
        Id = id;
        WalletId = walletId;
        Points = points;
        Type = type;
        ReferenceId = referenceId;
        PointsAfter = pointsAfter;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public int Points { get; private set; }
    public LoyaltyTransactionType Type { get; private set; }
    public string ReferenceId { get; private set; } = string.Empty;
    public int PointsAfter { get; private set; }
    public DateTime CreatedAt { get; private set; }
}

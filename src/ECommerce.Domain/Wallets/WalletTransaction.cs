namespace ECommerce.Domain.Wallets;

public enum WalletTransactionType
{
    Credit,
    Debit
}

public sealed class WalletTransaction
{
    private WalletTransaction() { } // EF Core

    public WalletTransaction(
        Guid id,
        Guid walletId,
        decimal amount,
        WalletTransactionType type,
        string referenceId,
        decimal balanceAfter)
    {
        Id = id;
        WalletId = walletId;
        Amount = amount;
        Type = type;
        ReferenceId = referenceId;
        BalanceAfter = balanceAfter;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public decimal Amount { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public string ReferenceId { get; private set; } = string.Empty;
    public decimal BalanceAfter { get; private set; }
    public DateTime CreatedAt { get; private set; }
}

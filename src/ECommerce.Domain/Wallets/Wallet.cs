using ECommerce.Domain.Common;

namespace ECommerce.Domain.Wallets;

public sealed class Wallet : BaseEntity<Guid>
{
    private readonly List<WalletTransaction> _walletTransactions = [];
    private readonly List<LoyaltyTransaction> _loyaltyTransactions = [];

    private Wallet() { } // EF Core

    public Guid CustomerId { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int LoyaltyPoints { get; private set; }

    public IReadOnlyCollection<WalletTransaction> WalletTransactions => _walletTransactions.AsReadOnly();
    public IReadOnlyCollection<LoyaltyTransaction> LoyaltyTransactions => _loyaltyTransactions.AsReadOnly();

    public static Wallet Create(Guid customerId, string currency = "USD")
    {
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Balance = 0,
            LoyaltyPoints = 0,
            Currency = currency
        };
        return wallet;
    }

    public Result Credit(decimal amount, string referenceId)
    {
        if (amount <= 0)
            return Result.Failure(new Error("Wallet.InvalidAmount", "Credit amount must be greater than zero.", ErrorType.Validation));

        Balance += amount;

        _walletTransactions.Add(new WalletTransaction(
            Guid.NewGuid(), Id, amount, WalletTransactionType.Credit, referenceId, Balance));

        return Result.Success();
    }

    public Result Debit(decimal amount, string referenceId)
    {
        if (amount <= 0)
            return Result.Failure(new Error("Wallet.InvalidAmount", "Debit amount must be greater than zero.", ErrorType.Validation));

        if (Balance < amount)
            return Result.Failure(WalletErrors.InsufficientFunds);

        Balance -= amount;

        _walletTransactions.Add(new WalletTransaction(
            Guid.NewGuid(), Id, amount, WalletTransactionType.Debit, referenceId, Balance));

        return Result.Success();
    }

    public Result AddPoints(int points, string referenceId)
    {
        if (points <= 0)
            return Result.Failure(new Error("Loyalty.InvalidPoints", "Points must be greater than zero.", ErrorType.Validation));

        LoyaltyPoints += points;

        _loyaltyTransactions.Add(new LoyaltyTransaction(
            Guid.NewGuid(), Id, points, LoyaltyTransactionType.Earned, referenceId, LoyaltyPoints));

        return Result.Success();
    }

    public Result RedeemPoints(int points, string referenceId)
    {
        if (points <= 0)
            return Result.Failure(new Error("Loyalty.InvalidPoints", "Points must be greater than zero.", ErrorType.Validation));

        if (LoyaltyPoints < points)
            return Result.Failure(WalletErrors.InsufficientPoints);

        LoyaltyPoints -= points;

        _loyaltyTransactions.Add(new LoyaltyTransaction(
            Guid.NewGuid(), Id, points, LoyaltyTransactionType.Redeemed, referenceId, LoyaltyPoints));

        return Result.Success();
    }
}

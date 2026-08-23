using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Wallets;

public static class WalletErrors
{
    public static readonly Error NotFound = new(
        "Wallet.NotFound",
        "The wallet was not found.",
        ErrorType.NotFound);

    public static readonly Error InsufficientFunds = new(
        "Wallet.InsufficientFunds",
        "The wallet does not have enough funds for this transaction.",
        ErrorType.Validation);

    public static readonly Error InsufficientPoints = new(
        "Wallet.InsufficientPoints",
        "The wallet does not have enough loyalty points for this transaction.",
        ErrorType.Validation);
}

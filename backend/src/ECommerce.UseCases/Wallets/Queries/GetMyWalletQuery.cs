
namespace ECommerce.UseCases.Wallets.Queries;

public sealed record GetMyWalletQuery() : IRequest<Result<WalletResponse>>;

public sealed record WalletResponse(
    decimal Balance,
    string Currency,
    int LoyaltyPoints);

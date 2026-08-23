using ECommerce.Domain.Wallets;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wallets.Ports;

namespace ECommerce.UseCases.Wallets.Handlers;

public sealed class GetMyWalletQueryHandler(
    ICurrentUser currentUser,
    IWalletRepository wallets) : IRequestHandler<Queries.GetMyWalletQuery, Result<Queries.WalletResponse>>
{
    public async Task<Result<Queries.WalletResponse>> Handle(Queries.GetMyWalletQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result<Queries.WalletResponse>.Failure(ECommerce.Domain.Wallets.WalletErrors.NotFound);
        }

        var wallet = await wallets.GetByCustomerIdAsNoTrackingAsync(currentUser.UserId.Value, cancellationToken);

        return wallet is null
            ? Result<Queries.WalletResponse>.Success(new Queries.WalletResponse(0, "USD", 0))
            : Result<Queries.WalletResponse>.Success(new Queries.WalletResponse(
                wallet.Balance,
                wallet.Currency,
                wallet.LoyaltyPoints));
    }
}

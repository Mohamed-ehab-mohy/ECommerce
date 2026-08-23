using ECommerce.Domain.Wallets;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wallets.Ports;

namespace ECommerce.UseCases.Wallets.Handlers;

public sealed class DepositToWalletCommandHandler(
    ICurrentUser currentUser,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork) : IRequestHandler<Commands.DepositToWalletCommand, Result>
{
    public async Task<Result> Handle(Commands.DepositToWalletCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure(ECommerce.Domain.Wallets.WalletErrors.NotFound);
        }

        var wallet = await wallets.GetByCustomerIdAsync(currentUser.UserId.Value, cancellationToken);
        if (wallet is null)
        {
            wallet = Wallet.Create(currentUser.UserId.Value, "USD");
            await wallets.AddAsync(wallet, cancellationToken);
        }

        var result = wallet.Credit(request.Amount, $"Deposit_{Guid.NewGuid()}");
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

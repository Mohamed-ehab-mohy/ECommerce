using ECommerce.Domain.Wallets;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Wallets.Ports;

namespace ECommerce.UseCases.Wallets.Handlers;

public sealed class ConvertPointsCommandHandler(
    ICurrentUser currentUser,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork) : IRequestHandler<Commands.ConvertPointsCommand, Result>
{
    private const int PointsPerDollar = 100;

    public async Task<Result> Handle(Commands.ConvertPointsCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure(WalletErrors.NotFound);
        }

        var wallet = await wallets.GetByCustomerIdAsync(currentUser.UserId.Value, cancellationToken);
        if (wallet is null)
        {
            return Result.Failure(WalletErrors.NotFound);
        }

        var redeemResult = wallet.RedeemPoints(request.Points, $"Convert_{Guid.NewGuid()}");
        if (redeemResult.IsFailure)
        {
            return redeemResult;
        }

        var amountToCredit = (decimal)request.Points / PointsPerDollar;
        var creditResult = wallet.Credit(amountToCredit, $"Convert_{Guid.NewGuid()}");
        
        if (creditResult.IsFailure)
        {
            return creditResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

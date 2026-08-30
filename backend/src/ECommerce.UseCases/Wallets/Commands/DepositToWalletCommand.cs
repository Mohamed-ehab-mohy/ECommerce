using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Wallets.Commands;

public sealed record DepositToWalletCommand(decimal Amount) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.WalletDeposit;
}

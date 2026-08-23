
namespace ECommerce.UseCases.Wallets.Commands;

public sealed record DepositToWalletCommand(decimal Amount) : IRequest<Result>;

using ECommerce.Domain.Payments;
using ECommerce.Domain.Wallets;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Wallets;

public sealed class WalletPaymentProvider(ECommerceDbContext dbContext) : IPaymentProvider
{
    public string Key => "Wallet";

    public async Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        // For wallet, we just check if the user has enough balance and return a dummy client token
        if (request.CustomerId is null)
        {
            return new PaymentIntentResult(false, string.Empty, string.Empty, null, "Wallet.CustomerRequired");
        }

        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(w => w.CustomerId == request.CustomerId, cancellationToken);
        if (wallet is null)
        {
            return new PaymentIntentResult(false, string.Empty, string.Empty, null, WalletErrors.NotFound.Code);
        }

        if (wallet.Balance < request.Amount)
        {
            return new PaymentIntentResult(false, string.Empty, string.Empty, null, WalletErrors.InsufficientFunds.Code);
        }

        // Use the wallet Id as provider reference/token
        var token = $"wallet_token_{wallet.Id}_{Guid.NewGuid()}";
        return new PaymentIntentResult(true, token, token, null, null);
    }

    public async Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken)
    {
        // ProviderToken contains wallet id from CreateIntentAsync: wallet_token_{walletId}_{guid}
        if (!request.ProviderToken.StartsWith("wallet_token_"))
        {
            return new PaymentAuthorizationResult(false, string.Empty, "Wallet.InvalidToken");
        }

        var parts = request.ProviderToken.Split('_');
        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var walletId))
        {
            return new PaymentAuthorizationResult(false, string.Empty, "Wallet.InvalidToken");
        }

        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);
        if (wallet is null)
        {
            return new PaymentAuthorizationResult(false, string.Empty, WalletErrors.NotFound.Code);
        }

        var result = wallet.Debit(request.Amount, request.IdempotencyKey);
        if (result.IsFailure)
        {
            return new PaymentAuthorizationResult(false, string.Empty, result.Error.Code);
        }

        // Save changes immediately since authorization handles the actual deduction
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PaymentAuthorizationResult(true, request.IdempotencyKey, null);
    }

    public async Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken)
    {
        // To refund, we need to know the wallet. The provider reference is the IdempotencyKey of the authorization.
        // We look up the original transaction to find the wallet.
        var transaction = await dbContext.WalletTransactions
            .FirstOrDefaultAsync(t => t.ReferenceId == request.ProviderReference && t.Type == WalletTransactionType.Debit, cancellationToken);

        if (transaction is null)
        {
            return new PaymentRefundResult(false, null, "Wallet.TransactionNotFound");
        }

        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == transaction.WalletId, cancellationToken);
        if (wallet is null)
        {
            return new PaymentRefundResult(false, null, WalletErrors.NotFound.Code);
        }

        var result = wallet.Credit(request.Amount, request.IdempotencyKey);
        if (result.IsFailure)
        {
            return new PaymentRefundResult(false, null, result.Error.Code);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PaymentRefundResult(true, request.IdempotencyKey, null);
    }

    public Task<IReadOnlyList<ProviderTransaction>> ListTransactionsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        // Wallet funds are internal and do not require external provider reconciliation,
        // so there is no external transaction ledger to list.
        return Task.FromResult<IReadOnlyList<ProviderTransaction>>([]);
    }
}

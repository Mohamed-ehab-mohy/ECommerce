using ECommerce.UseCases.Payments.Services;
using Hangfire;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>
/// Nightly reconciliation job: provider statement vs platform ledger, flags drift for finance
/// (US-I-005, US-I-007, T-DAT-015).
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class NightlyReconciliationJob(ReconciliationService service)
{
    public const string Schedule = "0 2 * * *";

    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        service.RunAsync(cancellationToken);
}

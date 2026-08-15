using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Responses;
using ECommerce.UseCases.Payments.Services;
using MediatR;

namespace ECommerce.UseCases.Payments.Handlers;

/// <summary>
/// Runs the nightly reconciliation feed on demand: snapshots unreconciled payments, compares each
/// pending record against the originating provider's transactions, flags drift, and writes the
/// financial audit trail (US-I-005, US-I-007, T-DAT-015).
/// </summary>
public sealed class RunReconciliationCommandHandler(ReconciliationService service)
    : IRequestHandler<RunReconciliationCommand, Result<ReconciliationRunResponse>>
{
    public async Task<Result<ReconciliationRunResponse>> Handle(
        RunReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        var report = await service.RunAsync(cancellationToken);
        return report;
    }
}

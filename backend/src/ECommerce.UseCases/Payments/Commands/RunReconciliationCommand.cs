using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;

namespace ECommerce.UseCases.Payments.Commands;

/// <summary>Triggers a provider-vs-platform reconciliation run.</summary>
public sealed class RunReconciliationCommand : IRequest<Result<ReconciliationRunResponse>>, IRequirePermission
{
    public string Permission => Permissions.FinanceReconcile;
}

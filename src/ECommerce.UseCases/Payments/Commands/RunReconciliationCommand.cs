using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Responses;
using MediatR;

namespace ECommerce.UseCases.Payments.Commands;

/// <summary>Triggers a provider-vs-platform reconciliation run (US-I-005, US-I-007, T-DAT-015).</summary>
public sealed class RunReconciliationCommand : IRequest<Result<ReconciliationRunResponse>>, IRequirePermission
{
    public string Permission => Permissions.FinanceReconcile;
}

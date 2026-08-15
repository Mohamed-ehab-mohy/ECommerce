using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Handlers;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class RunReconciliationCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakePaymentProviderFactory _providerFactory = new();

    private readonly FakeAuditLogWriter _audit = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void Command_Requires_Finance_Reconcile_Permission()
    {
        var command = new RunReconciliationCommand();

        Assert.Equal(Permissions.FinanceReconcile, command.Permission);
    }

    [Fact]
    public async Task Handle_Returns_Reconciliation_Report()
    {
        var service = new ECommerce.UseCases.Payments.Services.ReconciliationService(
            _payments,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            _providerFactory,
            _audit,
            NullLogger<ECommerce.UseCases.Payments.Services.ReconciliationService>.Instance);
        var handler = new RunReconciliationCommandHandler(service);

        var result = await handler.Handle(new RunReconciliationCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(0, result.Value.MatchedCount);
        Assert.Equal(0, result.Value.DriftCount);
        Assert.Equal(UtcNow, result.Value.CheckedAtUtc);
    }
}

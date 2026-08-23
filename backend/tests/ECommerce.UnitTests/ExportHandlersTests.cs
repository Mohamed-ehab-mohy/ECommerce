using ECommerce.Domain.Reporting;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Commands;
using ECommerce.UseCases.Reports.Handlers;
using ECommerce.UseCases.Reports.Ports;

namespace ECommerce.UnitTests;

public sealed class ExportHandlersTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeExportJobRepository _exports = new();

    private readonly FakeExportJobScheduler _scheduler = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private CreateExportCommandHandler CreateHandler(Guid? userId = null) =>
        new(
            _exports,
            _scheduler,
            _unitOfWork,
            new CreateExportCommandValidator(),
            new FakeCurrentUser(userId: userId),
            new FixedTimeProvider(UtcNow));

    [Fact]
    public async Task Create_Queues_Export_Job_With_Serialized_Filters()
    {
        var userId = Guid.NewGuid();
        var handler = CreateHandler(userId);

        var result = await handler.Handle(
            new CreateExportCommand(ExportReportTypes.Sales, null, null, "week", "USD"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var job = Assert.Single(_exports.Jobs);
        Assert.Equal(result.Value.ExportId, job.Id);
        Assert.Equal(ExportReportTypes.Sales, job.ReportType);
        Assert.Equal(userId, job.CreatedBy);
        Assert.Equal(ExportJobStatus.Queued, job.Status);
        Assert.Contains("\"granularity\":\"week\"", job.FiltersJson);
        Assert.Equal(job.Id, Assert.Single(_scheduler.Enqueued));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_Normalizes_Report_Type_To_Lowercase()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateExportCommand("SALES", null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ExportReportTypes.Sales, Assert.Single(_exports.Jobs).ReportType);
    }

    [Fact]
    public async Task Create_Rejects_Unsupported_Report_Type()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateExportCommand("bogus", null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_exports.Jobs);
        Assert.Empty(_scheduler.Enqueued);
    }

    [Fact]
    public async Task Create_Rejects_Range_Longer_Than_400_Days()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateExportCommand(
                ExportReportTypes.Sales,
                UtcNow.AddDays(-401),
                UtcNow,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Get_Returns_Status_With_File_Key_When_Completed()
    {
        var job = ExportJob.Create(ExportReportTypes.Inventory, "{}", null, UtcNow);
        job.MarkRunning(UtcNow.AddSeconds(1));
        job.Complete(42, "exports/inventory.csv", UtcNow.AddSeconds(2));
        _exports.Add(job);
        var handler = new GetExportQueryHandler(_exports, new GetExportQueryValidator());

        var result = await handler.Handle(new GetExportQuery(job.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ExportJobStatus.Completed.ToString(), result.Value.Status);
        Assert.Equal(42, result.Value.RowCount);
        Assert.Equal("exports/inventory.csv", result.Value.FileKey);
    }

    [Fact]
    public async Task Get_Unknown_Export_Is_Not_Found()
    {
        var handler = new GetExportQueryHandler(_exports, new GetExportQueryValidator());

        var result = await handler.Handle(new GetExportQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ExportErrors.ExportNotFound, result.Error);
    }

    [Fact]
    public void Commands_Require_Reports_Read_Permission()
    {
        Assert.Equal(ECommerce.Shared.Authorization.Permissions.ReportsRead, new CreateExportCommand("sales", null, null, null, null).Permission);
        Assert.Equal(ECommerce.Shared.Authorization.Permissions.ReportsRead, new GetExportQuery(Guid.NewGuid()).Permission);
    }
}

using ECommerce.Domain.Reporting;

namespace ECommerce.UnitTests;

public sealed class ExportJobTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Starts_Queued_With_Filters()
    {
        var createdBy = Guid.NewGuid();
        var job = ExportJob.Create(ExportReportTypes.Sales, "{}", createdBy, UtcNow);

        Assert.Equal(ExportReportTypes.Sales, job.ReportType);
        Assert.Equal("{}", job.FiltersJson);
        Assert.Equal(createdBy, job.CreatedBy);
        Assert.Equal(ExportJobStatus.Queued, job.Status);
        Assert.Equal(UtcNow, job.CreatedAt);
        Assert.Null(job.FileKey);
    }

    [Fact]
    public void MarkRunning_Records_Start()
    {
        var job = ExportJob.Create(ExportReportTypes.Sales, "{}", null, UtcNow);
        var startedAt = UtcNow.AddSeconds(1);

        job.MarkRunning(startedAt);

        Assert.Equal(ExportJobStatus.Running, job.Status);
        Assert.Equal(startedAt, job.StartedAtUtc);
    }

    [Fact]
    public void Complete_Stores_Row_Count_And_File_Key()
    {
        var job = ExportJob.Create(ExportReportTypes.Inventory, "{}", null, UtcNow);
        var completedAt = UtcNow.AddSeconds(2);

        job.Complete(42, "exports/sales.csv", completedAt);

        Assert.Equal(ExportJobStatus.Completed, job.Status);
        Assert.Equal(42, job.RowCount);
        Assert.Equal("exports/sales.csv", job.FileKey);
        Assert.Equal(completedAt, job.CompletedAtUtc);
    }

    [Fact]
    public void Fail_Records_Completion_Without_File()
    {
        var job = ExportJob.Create(ExportReportTypes.Finance, "{}", null, UtcNow);
        var failedAt = UtcNow.AddSeconds(3);

        job.Fail(failedAt);

        Assert.Equal(ExportJobStatus.Failed, job.Status);
        Assert.Equal(failedAt, job.CompletedAtUtc);
        Assert.Null(job.FileKey);
    }
}

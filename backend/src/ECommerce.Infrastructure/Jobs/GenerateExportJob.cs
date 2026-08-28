using System.Text;
using System.Text.Json;
using ECommerce.Domain.Reporting;
using ECommerce.Infrastructure.Reports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports;
using ECommerce.UseCases.Reports.Handlers;
using ECommerce.UseCases.Reports.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>Generates a report export asynchronously and stores the CSV file.</summary>
[AutomaticRetry(Attempts = 2)]
public sealed class GenerateExportJob(
    IExportJobRepository exports,
    IReportingQueryService reporting,
    IExportFileStore fileStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<GenerateExportJob> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ExecuteAsync(Guid exportId, CancellationToken cancellationToken)
    {
        var export = await exports.GetByIdAsync(exportId, cancellationToken);
        if (export is null)
        {
            logger.LogWarning("Export {ExportId} not found; skipping.", exportId);
            return;
        }

        // Idempotent: an already-finished export is not re-run.
        if (export.Status is ExportJobStatus.Completed or ExportJobStatus.Failed)
        {
            logger.LogInformation("Export {ExportId} already processed; skipping.", exportId);
            return;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        export.MarkRunning(utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var filters = JsonSerializer.Deserialize<ExportFilters>(export.FiltersJson, JsonOptions)
                ?? new ExportFilters(null, null, null, null);
            var (from, to) = ReportRanges.Resolve(filters.From, filters.To, utcNow);

            var csv = await RenderAsync(export.ReportType, filters, from, to, cancellationToken);

            var fileKey = await fileStore.PutAsync(
                $"exports/{export.Id:N}.csv",
                Encoding.UTF8.GetBytes(csv),
                cancellationToken);

            export.Complete(CountRows(csv), fileKey, utcNow);
            logger.LogInformation("Export {ExportId} completed with {RowCount} rows.", export.Id, CountRows(csv));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Export {ExportId} failed.", export.Id);
            export.Fail(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> RenderAsync(
        string reportType,
        ExportFilters filters,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken) =>
        reportType switch
        {
            ExportReportTypes.Sales => CsvReportRenderer.RenderSales(await reporting.GetSalesSeriesAsync(
                new SalesReportFilter(from, to, filters.Granularity ?? "day", filters.Currency), cancellationToken)),
            ExportReportTypes.Inventory => CsvReportRenderer.RenderInventory(
                (await reporting.GetInventoryAsync(cancellationToken)).Lines),
            ExportReportTypes.Finance => CsvReportRenderer.RenderFinance(await reporting.GetFinanceAsync(
                new FinanceReportFilter(from, to), cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported report type '{reportType}'.")
        };

    private static int CountRows(string csv)
    {
        var lines = csv.Split('\n').Count(line => line.Length > 0);
        return Math.Max(lines - 1, 0);
    }
}

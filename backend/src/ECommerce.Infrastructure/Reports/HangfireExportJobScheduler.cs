using ECommerce.Infrastructure.Jobs;
using ECommerce.UseCases.Reports.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Reports;

/// <summary>Enqueues an export job into Hangfire.</summary>
public sealed class HangfireExportJobScheduler(
    IBackgroundJobClient? backgroundJobClient,
    ILogger<HangfireExportJobScheduler> logger) : IExportJobScheduler
{
    public void Enqueue(Guid exportId)
    {
        if (backgroundJobClient is null)
        {
            logger.LogInformation("Hangfire not configured; export {ExportId} will not be scheduled.", exportId);
            return;
        }

        backgroundJobClient.Enqueue<GenerateExportJob>(job => job.ExecuteAsync(exportId, CancellationToken.None));
    }
}

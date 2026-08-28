using ECommerce.Infrastructure.Jobs;
using ECommerce.UseCases.Catalog.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Catalog;

/// <summary>Enqueues a product import batch for the processing job.</summary>
public sealed class HangfireProductImportJobScheduler(
    IBackgroundJobClient? backgroundJobClient,
    ILogger<HangfireProductImportJobScheduler> logger) : IProductImportJobScheduler
{
    public void Enqueue(Guid importId)
    {
        if (backgroundJobClient is null)
        {
            logger.LogInformation("Hangfire not configured; import {ImportId} will not be processed.", importId);
            return;
        }

        backgroundJobClient.Enqueue<ProcessProductImportJob>(job => job.ExecuteAsync(importId, CancellationToken.None));
    }
}

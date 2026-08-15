using ECommerce.UseCases.Catalog.Services;
using Hangfire;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>Processes a bulk product import batch (US-B-007, BR-1207).</summary>
[AutomaticRetry(Attempts = 2)]
public sealed class ProcessProductImportJob(ProductImportService service)
{
    public Task ExecuteAsync(Guid importId, CancellationToken cancellationToken) =>
        service.ProcessAsync(importId, cancellationToken);
}

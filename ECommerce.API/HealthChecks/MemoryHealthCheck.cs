using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API.HealthChecks;

public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _threshold;

    public MemoryHealthCheck(long threshold = 1024L * 1024 * 1024)
    {
        _threshold = threshold;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedMemory = GC.GetTotalMemory(forceFullCollection: false);
        var gcMemory = GC.GetGCMemoryInfo();

        var memoryData = new Dictionary<string, object>
        {
            { "allocatedBytes", allocatedMemory },
            { "allocatedMB", Math.Round(allocatedMemory / 1024.0 / 1024.0, 2) },
            { "gen0Collections", GC.CollectionCount(0) },
            { "gen1Collections", GC.CollectionCount(1) },
            { "gen2Collections", GC.CollectionCount(2) },
            { "totalAvailableMemoryMB", Math.Round(gcMemory.TotalAvailableMemoryBytes / 1024.0 / 1024.0, 2) },
            { "heapSizeBytes", gcMemory.HeapSizeBytes },
            { "thresholdBytes", _threshold }
        };

        if (allocatedMemory > _threshold)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage ({allocatedMemory / 1024.0 / 1024.0:F2} MB) exceeds threshold ({_threshold / 1024.0 / 1024.0:F2} MB).",
                data: memoryData));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage ({allocatedMemory / 1024.0 / 1024.0:F2} MB) is within normal range.",
            data: memoryData));
    }
}

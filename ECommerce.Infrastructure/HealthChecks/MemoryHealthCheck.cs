using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.HealthChecks;

public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly long _thresholdBytes;
    private readonly ILogger<MemoryHealthCheck> _logger;

    public MemoryHealthCheck(IConfiguration configuration, ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("HealthChecks:Memory");
        var thresholdValue = section["ThresholdBytes"];
        _thresholdBytes = long.TryParse(thresholdValue, out var parsed) ? parsed : 1024L * 1024 * 1024;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedMemory = GC.GetTotalMemory(forceFullCollection: false);
        var gcMemory = GC.GetGCMemoryInfo();
        var allocatedMB = Math.Round(allocatedMemory / 1024.0 / 1024.0, 2);
        var thresholdMB = Math.Round(_thresholdBytes / 1024.0 / 1024.0, 2);

        var data = new Dictionary<string, object>
        {
            { "allocatedBytes", allocatedMemory },
            { "allocatedMB", allocatedMB },
            { "thresholdBytes", _thresholdBytes },
            { "thresholdMB", thresholdMB },
            { "gen0Collections", GC.CollectionCount(0) },
            { "gen1Collections", GC.CollectionCount(1) },
            { "gen2Collections", GC.CollectionCount(2) },
            { "totalAvailableMemoryMB", Math.Round(gcMemory.TotalAvailableMemoryBytes / 1024.0 / 1024.0, 2) },
            { "heapSizeBytes", gcMemory.HeapSizeBytes }
        };

        if (allocatedMemory > _thresholdBytes)
        {
            _logger.LogWarning(
                "Memory usage ({AllocatedMB} MB) exceeds threshold ({ThresholdMB} MB).",
                allocatedMB,
                thresholdMB);

            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage ({allocatedMB} MB) exceeds threshold ({thresholdMB} MB).",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage ({allocatedMB} MB) is within normal range.",
            data: data));
    }
}

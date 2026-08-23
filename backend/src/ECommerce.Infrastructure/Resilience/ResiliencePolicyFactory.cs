using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ECommerce.Infrastructure.Resilience;

public static class ResiliencePolicyFactory
{
    public static ResiliencePipeline CreatePspPipeline() =>
        CreateBuilder("psp", retryCount: 3, breakAfter: 5, timeoutSeconds: 30).Build();

    public static ResiliencePipeline CreateCarrierPipeline() =>
        CreateBuilder("carrier", retryCount: 2, breakAfter: 3, timeoutSeconds: 60).Build();

    public static ResiliencePipeline CreateDefaultPipeline() =>
        CreateBuilder("default", retryCount: 2, breakAfter: 5, timeoutSeconds: 15).Build();

    private static ResiliencePipelineBuilder CreateBuilder(
        string name,
        int retryCount,
        int breakAfter,
        int timeoutSeconds)
    {
        var builder = new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                Name = $"{name}-timeout"
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = retryCount,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Name = $"{name}-retry"
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = breakAfter,
                BreakDuration = TimeSpan.FromSeconds(30),
                Name = $"{name}-circuit"
            });

        return builder;
    }
}

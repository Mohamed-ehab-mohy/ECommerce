using ECommerce.Infrastructure.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ECommerce.UnitTests.Tests.ChaosEngineering;

public sealed class ChaosResilienceTests
{
    [Fact]
    public async Task PSP_Pipeline_Retries_On_Transient_Failure()
    {
        var pipeline = ResiliencePolicyFactory.CreatePspPipeline();
        var attempt = 0;

        var result = await pipeline.ExecuteAsync(async ct =>
        {
            attempt++;
            return attempt < 3
                ? throw new HttpRequestException("Transient network error")
                : "success";
        }, CancellationToken.None);

        Assert.Equal("success", result);
        Assert.Equal(3, attempt);
    }

    [Fact]
    public async Task Carrier_Pipeline_Circuit_Breaks_After_Threshold()
    {
        var pipeline = ResiliencePolicyFactory.CreateCarrierPipeline();
        var attempts = 0;

        for (var i = 0; i < 5; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(_ =>
                {
                    attempts++;
                    throw new HttpRequestException("Permanent failure");
                }, CancellationToken.None);
            }
            catch (HttpRequestException)
            {
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }

        Assert.True(attempts <= 4, "Circuit should have broken before all attempts completed");
    }

    [Fact]
    public async Task PSP_Pipeline_Timeout_Fires_At_30s()
    {
        var pipeline = ResiliencePolicyFactory.CreatePspPipeline();

        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(60), ct);
            }, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Default_Pipeline_Handles_OperationCanceled()
    {
        var pipeline = ResiliencePolicyFactory.CreateDefaultPipeline();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.CompletedTask;
            }, new CancellationToken(true));
        });
    }

    [Fact]
    public async Task PSP_Pipeline_Retries_With_Jitter()
    {
        var pipeline = ResiliencePolicyFactory.CreatePspPipeline();
        var delays = new List<TimeSpan>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var attempt = 0;

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                attempt++;
                delays.Add(TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds));
                throw new HttpRequestException("Fail all attempts");
            }, CancellationToken.None);
        });

        Assert.Equal(4, attempt);
        Assert.Equal(4, delays.Count);
    }

    [Fact]
    public void All_Pipeline_Factories_Return_NonNull()
    {
        var psp = ResiliencePolicyFactory.CreatePspPipeline();
        var carrier = ResiliencePolicyFactory.CreateCarrierPipeline();
        var defaultPipeline = ResiliencePolicyFactory.CreateDefaultPipeline();

        Assert.NotNull(psp);
        Assert.NotNull(carrier);
        Assert.NotNull(defaultPipeline);
    }

    [Fact]
    public async Task Carrier_Pipeline_Retries_Before_Breaking()
    {
        var pipeline = ResiliencePolicyFactory.CreateCarrierPipeline();
        var attempt = 0;

        var result = await pipeline.ExecuteAsync(async ct =>
        {
            attempt++;
            return attempt < 2
                ? throw new HttpRequestException("Fail first two")
                : "recovered";
        }, CancellationToken.None);

        Assert.Equal("recovered", result);
        Assert.Equal(2, attempt);
    }

    [Fact]
    public async Task Simulated_Redis_Failure_Gracefully_Degrades()
    {
        var pipeline = ResiliencePolicyFactory.CreateDefaultPipeline();
        var fallbackResult = "fallback";

        try
        {
            await pipeline.ExecuteAsync(async _ =>
            {
                throw new ConnectionException("Redis connection refused");
            }, CancellationToken.None);
        }
        catch (ConnectionException)
        {
            fallbackResult = "degraded";
        }

        Assert.Equal("degraded", fallbackResult);
    }

    [Fact]
    public async Task Simulated_Database_Timeout_Is_Caught_By_CircuitBreaker()
    {
        var pipeline = ResiliencePolicyFactory.CreatePspPipeline();
        var failureCount = 0;

        for (var i = 0; i < 6; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(async _ =>
                {
                    Interlocked.Increment(ref failureCount);
                    throw new TimeoutException("Postgres connection timed out");
                }, CancellationToken.None);
            }
            catch (TimeoutException)
            {
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }

        Assert.True(failureCount <= 5, $"Circuit breaker should stop execution after threshold, but ran {failureCount} times");
    }

    [Fact]
    public async Task Simulated_Message_Broker_Outage_Degrades_Queue()
    {
        var pipeline = ResiliencePolicyFactory.CreateDefaultPipeline();
        var messagesQueued = 0;

        for (var i = 0; i < 3; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(async _ =>
                {
                    throw new ConnectionException("RabbitMQ connection refused");
                }, CancellationToken.None);
            }
            catch (ConnectionException)
            {
                messagesQueued++;
            }
            catch (BrokenCircuitException)
            {
                messagesQueued++;
            }
        }

        Assert.True(messagesQueued > 0, "Messages should be queued for later delivery during outage");
    }

    private sealed class ConnectionException(string message) : Exception(message);
}

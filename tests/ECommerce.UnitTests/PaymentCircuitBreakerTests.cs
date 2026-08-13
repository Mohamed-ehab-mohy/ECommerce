using ECommerce.Infrastructure.Payments;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests;

public sealed class PaymentCircuitBreakerTests
{
    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static readonly DateTimeOffset Start = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    private readonly MutableTimeProvider _time = new(Start);

    private readonly PaymentCircuitBreaker _breaker;

    public PaymentCircuitBreakerTests()
    {
        var options = Options.Create(new PaymentProviderOptions
        {
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailureThreshold = 3,
                Cooldown = TimeSpan.FromSeconds(60)
            }
        });

        _breaker = new PaymentCircuitBreaker(options, _time);
    }

    [Fact]
    public void IsAvailable_Initially()
    {
        Assert.True(_breaker.IsAvailable("psp-a"));
    }

    [Fact]
    public void Stays_Available_Below_Threshold()
    {
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");

        Assert.True(_breaker.IsAvailable("psp-a"));
    }

    [Fact]
    public void Opens_After_Threshold_Failures()
    {
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");

        Assert.False(_breaker.IsAvailable("psp-a"));
    }

    [Fact]
    public void Resets_On_Success()
    {
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");
        _breaker.RecordSuccess("psp-a");
        _breaker.RecordFailure("psp-a");

        Assert.True(_breaker.IsAvailable("psp-a"));
    }

    [Fact]
    public void Allows_Half_Open_Trial_After_Cooldown()
    {
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");
        Assert.False(_breaker.IsAvailable("psp-a"));

        _time.Now = Start.AddSeconds(61);

        Assert.True(_breaker.IsAvailable("psp-a"));
    }

    [Fact]
    public void Half_Open_Failure_Reopens_With_Fresh_Cooldown()
    {
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");

        _time.Now = Start.AddSeconds(61);
        Assert.True(_breaker.IsAvailable("psp-a"));

        _breaker.RecordFailure("psp-a");

        Assert.False(_breaker.IsAvailable("psp-a"));

        _time.Now = Start.AddSeconds(62);
        Assert.False(_breaker.IsAvailable("psp-a"));
    }

    [Fact]
    public void Tracks_Providers_Independently()
    {
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");
        _breaker.RecordFailure("psp-a");

        Assert.False(_breaker.IsAvailable("psp-a"));
        Assert.True(_breaker.IsAvailable("psp-b"));
    }
}

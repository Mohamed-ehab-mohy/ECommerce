using ECommerce.UseCases.Identity;

namespace ECommerce.UnitTests;

public sealed class LoginAttemptThrottlerTests
{
    private static readonly AuthSettings Settings = new()
    {
        MaxFailedLoginAttemptsPerIp = 3,
        LoginAttemptWindowMinutes = 5
    };

    [Fact]
    public void IsBlocked_False_Initially()
    {
        var throttler = new InMemoryLoginAttemptThrottler(Settings);

        Assert.False(throttler.IsBlocked("203.0.113.1", DateTime.UtcNow, out var retryAfter));
        Assert.Equal(0, retryAfter);
    }

    [Fact]
    public void Blocks_When_Attempts_Reach_Limit_In_Window()
    {
        var throttler = new InMemoryLoginAttemptThrottler(Settings);
        var utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.False(throttler.IsBlocked("203.0.113.2", utcNow, out _));

        throttler.RecordFailure("203.0.113.2", utcNow);
        throttler.RecordFailure("203.0.113.2", utcNow);
        Assert.False(throttler.IsBlocked("203.0.113.2", utcNow, out _));

        throttler.RecordFailure("203.0.113.2", utcNow);
        Assert.True(throttler.IsBlocked("203.0.113.2", utcNow, out var retryAfter));
        Assert.Equal(300, retryAfter);
    }

    [Fact]
    public void Block_Expires_After_Window_Elapses()
    {
        var throttler = new InMemoryLoginAttemptThrottler(Settings);
        var windowStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            throttler.RecordFailure("203.0.113.3", windowStart);
        }

        Assert.True(throttler.IsBlocked("203.0.113.3", windowStart.AddMinutes(4), out _));
        Assert.False(throttler.IsBlocked("203.0.113.3", windowStart.AddMinutes(6), out _));
    }

    [Fact]
    public void RetryAfter_Counts_Down_Within_Window()
    {
        var throttler = new InMemoryLoginAttemptThrottler(Settings);
        var windowStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            throttler.RecordFailure("203.0.113.4", windowStart);
        }

        Assert.True(throttler.IsBlocked("203.0.113.4", windowStart.AddMinutes(2), out var retryAfter));
        Assert.Equal(180, retryAfter);
    }

    [Fact]
    public void Success_Resets_Counter()
    {
        var throttler = new InMemoryLoginAttemptThrottler(Settings);
        var utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        throttler.RecordFailure("203.0.113.5", utcNow);
        throttler.RecordFailure("203.0.113.5", utcNow);
        throttler.RecordSuccess("203.0.113.5", utcNow);

        Assert.False(throttler.IsBlocked("203.0.113.5", utcNow, out _));

        throttler.RecordFailure("203.0.113.5", utcNow);
        throttler.RecordFailure("203.0.113.5", utcNow);
        Assert.False(throttler.IsBlocked("203.0.113.5", utcNow, out _));
    }

    [Fact]
    public void Counters_Are_Scoped_Per_Ip()
    {
        var throttler = new InMemoryLoginAttemptThrottler(Settings);
        var utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            throttler.RecordFailure("203.0.113.6", utcNow);
        }

        Assert.True(throttler.IsBlocked("203.0.113.6", utcNow, out _));
        Assert.False(throttler.IsBlocked("203.0.113.7", utcNow, out _));
    }
}

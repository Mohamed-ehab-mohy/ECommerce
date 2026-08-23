using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity;

public sealed class InMemoryLoginAttemptThrottler(AuthSettings settings) : ILoginAttemptThrottler
{
    private readonly int _maxAttempts = settings.MaxFailedLoginAttemptsPerIp;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(settings.LoginAttemptWindowMinutes);
    private readonly Dictionary<string, (int Count, DateTime WindowStartUtc)> _windows = new();
    private readonly object _lock = new();

    public bool IsBlocked(string clientIp, DateTime utcNow, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        lock (_lock)
        {
            if (_windows.TryGetValue(clientIp, out var entry) &&
                utcNow < entry.WindowStartUtc + _window &&
                entry.Count >= _maxAttempts)
            {
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(((entry.WindowStartUtc + _window) - utcNow).TotalSeconds));
                return true;
            }
        }

        return false;
    }

    public void RecordFailure(string clientIp, DateTime utcNow)
    {
        lock (_lock)
        {
            if (!_windows.TryGetValue(clientIp, out var entry) || utcNow >= entry.WindowStartUtc + _window)
            {
                entry = (0, utcNow);
            }

            _windows[clientIp] = (entry.Count + 1, entry.WindowStartUtc);
        }
    }

    public void RecordSuccess(string clientIp, DateTime utcNow)
    {
        lock (_lock)
        {
            _windows.Remove(clientIp);
        }
    }
}

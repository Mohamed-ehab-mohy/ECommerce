namespace ECommerce.Infrastructure.Common;

public static class TenantScope
{
    private static readonly AsyncLocal<Guid?> _current = new();

    public static Guid? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static IDisposable Begin(Guid? tenantId)
    {
        var previous = Current;
        Current = tenantId;
        return new TenantScopeHandle(previous);
    }

    private sealed class TenantScopeHandle(Guid? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Current = previous;
                _disposed = true;
            }
        }
    }
}

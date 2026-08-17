namespace ECommerce.Infrastructure.Outbox;

/// <summary>
/// Collects actions to execute after the outbox transaction commits.
/// Prevents the race condition where Hangfire jobs run before delivery rows exist.
/// </summary>
public sealed class PostCommitActions
{
    private readonly List<Func<Task>> _actions = [];

    public void Add(Func<Task> action) => _actions.Add(action);

    public async ValueTask ExecuteAsync()
    {
        foreach (var action in _actions)
        {
            try
            {
                await action();
            }
            catch
            {
                // Fire-and-forget: enqueue failures after commit are logged by the scheduler.
            }
        }

        _actions.Clear();
    }
}

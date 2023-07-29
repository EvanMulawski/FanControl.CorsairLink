namespace CorsairLink;

public sealed class ExclusiveMonitor
{
    public event EventHandler? TaskCompleted;
    private readonly ExclusiveTaskRunner _taskRunner = new();

    public void WaitNonBlocking(Action action)
    {
        // fire-and-forget - we don't care about the result or handling an exception
        _ = _taskRunner.TryRunAsync(() => Task.Run(action), () => OnTaskCompleted(EventArgs.Empty));
    }

    private void OnTaskCompleted(EventArgs e)
    {
        TaskCompleted?.Invoke(this, e);
    }
}
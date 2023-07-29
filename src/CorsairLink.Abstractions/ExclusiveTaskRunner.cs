namespace CorsairLink;

public sealed class ExclusiveTaskRunner
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Task? _currentTask;

    public async Task TryRunAsync(Func<Task> taskFunc, Action completionAction)
    {
        if (_semaphore.Wait(0))
        {
            try
            {
                _currentTask = taskFunc.Invoke();
                await _currentTask;
                completionAction();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

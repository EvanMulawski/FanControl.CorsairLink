namespace CorsairLink;

public class ExclusiveTaskRunner
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Task? _currentTask;

    public async Task<bool> TryRunAsync(Func<Task> taskFunc)
    {
        if (_semaphore.Wait(0))
        {
            try
            {
                _currentTask = taskFunc.Invoke();
                await _currentTask;
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return false;
    }

    public bool TryRunSync(Func<Task> taskFunc)
    {
        if (_semaphore.Wait(0))
        {
            try
            {
                _currentTask = taskFunc.Invoke();
                _currentTask.GetAwaiter().GetResult();
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        return false;
    }
}

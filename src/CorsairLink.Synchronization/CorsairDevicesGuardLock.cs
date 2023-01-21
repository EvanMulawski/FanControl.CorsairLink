namespace CorsairLink.Synchronization;

internal sealed class CorsairDevicesGuardLock : IDisposable
{
    public CorsairDevicesGuardLock()
    {
        CorsairDevicesGuard.Acquire();
    }

    public void Dispose()
    {
        CorsairDevicesGuard.Release();
    }
}


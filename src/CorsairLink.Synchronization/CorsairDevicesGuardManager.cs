namespace CorsairLink.Synchronization;

public class CorsairDevicesGuardManager : IDeviceGuardManager
{
    public IDisposable AwaitExclusiveAccess()
    {
        return new CorsairDevicesGuardLock();
    }
}

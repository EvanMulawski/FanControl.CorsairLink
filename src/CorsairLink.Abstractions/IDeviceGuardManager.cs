namespace CorsairLink;

public interface IDeviceGuardManager
{
    IDisposable AwaitExclusiveAccess();
}

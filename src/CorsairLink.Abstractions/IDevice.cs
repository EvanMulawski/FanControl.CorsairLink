namespace CorsairLink;

public interface IDevice : IDeviceInfo
{
    bool IsConnected { get; }

    void Connect();
    void Disconnect();
}

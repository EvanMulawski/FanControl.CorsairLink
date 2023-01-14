namespace CorsairLink;

public interface IDevice : IDeviceInfo
{
    bool Connect();
    void Disconnect();
}

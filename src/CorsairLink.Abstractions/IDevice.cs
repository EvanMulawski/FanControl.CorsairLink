namespace CorsairLink;

public interface IDevice
{
    bool IsConnected { get; }
    string DevicePath { get; }

    void Connect();
    void Disconnect();
}

namespace CorsairLink.Devices.ICueLink;

public class LinkHubConnectedDevice
{
    public LinkHubConnectedDevice(int channel, string id, byte type, byte model)
    {
        Channel = channel;
        Id = id;
        Type = type;
        Model = model;
    }

    public int Channel { get; }
    public string Id { get; }
    public byte Type { get; }
    public byte Model { get; }
}

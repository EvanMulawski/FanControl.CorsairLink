namespace CorsairLink.Devices.ICueLink;

public class LinkHubConnectedDevice
{
    public LinkHubConnectedDevice(int channel, string id, byte model, byte variant)
    {
        Channel = channel;
        Id = id;
        Model = model;
        Variant = variant;
    }

    public int Channel { get; }
    public string Id { get; }
    public byte Model { get; }
    public byte Variant { get; }
}

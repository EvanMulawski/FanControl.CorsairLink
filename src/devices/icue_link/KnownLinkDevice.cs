namespace CorsairLink.Devices.ICueLink;

public sealed class KnownLinkDevice
{
    public KnownLinkDevice(LinkDeviceType type, byte model, string name, LinkDeviceFlags flags = LinkDeviceFlags.None)
    {
        Type = type;
        Model = model;
        Name = name;
        Flags = flags;
    }

    public LinkDeviceType Type { get; }
    public byte Model { get; }
    public string Name { get; }
    public LinkDeviceFlags Flags { get; }
}

public enum LinkDeviceType : byte
{
    FanQxSeries = 0x01,
    LiquidCooler = 0x07,
    WaterBlock = 0x09,
    FanRxSeries = 0x13,
}

[Flags]
public enum LinkDeviceFlags
{
    None = 0,
    ReportsTemperature = 1,
    ReportsSpeed = 2,
    ControlsSpeed = 4,
    All = ReportsTemperature | ReportsSpeed | ControlsSpeed,
}

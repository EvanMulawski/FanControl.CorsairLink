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

    public bool IsPump => Type == LinkDeviceType.LiquidCoolerHSeries || Type == LinkDeviceType.LiquidCoolerTitanSeries || Type == LinkDeviceType.Pump;
}

public enum LinkDeviceType : byte
{
    FanQxSeries = 0x01,
    FanLxSeries = 0x02,
    LiquidCoolerHSeries = 0x07,
    WaterBlock = 0x09,
    Pump = 0x0c,
    FanRxRgbSeries = 0x0f,
    LiquidCoolerTitanSeries = 0x11,
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

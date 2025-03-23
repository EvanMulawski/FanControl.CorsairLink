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
    FanRxMaxRgbSeries = 0x03,
    FanRxMaxSeries = 0x04,
    LiquidCoolerHSeries = 0x07,
    WaterBlockXc7Series = 0x09,
    WaterBlockXg3Series = 0x0a,
    Pump = 0x0c,
    FanRxRgbSeries = 0x0f,
    CapSwapModuleVrmFan = 0x10,
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

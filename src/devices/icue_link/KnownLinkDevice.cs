namespace CorsairLink.Devices.ICueLink;

public sealed class KnownLinkDevice
{
    public KnownLinkDevice(LinkDeviceModel model, byte variant, string name, LinkDeviceFlags flags = LinkDeviceFlags.None)
    {
        Model = model;
        Variant = variant;
        Name = name;
        Flags = flags;
    }

    public LinkDeviceModel Model { get; }
    public byte Variant { get; }
    public string Name { get; }
    public LinkDeviceFlags Flags { get; }

    public bool IsPump => Model == LinkDeviceModel.LiquidCoolerHSeries || Model == LinkDeviceModel.LiquidCoolerTitanSeries || Model == LinkDeviceModel.PumpXd5Series;
}

public enum LinkDeviceModel : byte
{
    FanQxSeries = 0x01,
    FanLxSeries = 0x02,
    FanRxMaxRgbSeries = 0x03,
    FanRxMaxSeries = 0x04,
    LiquidCoolerHSeries = 0x07,
    WaterBlockXc7Series = 0x09,
    WaterBlockXg3Series = 0x0a,
    PumpXd5Series = 0x0c,
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

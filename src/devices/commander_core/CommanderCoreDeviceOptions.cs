namespace CorsairLink.Devices;

public class CommanderCoreDeviceOptions
{
    public static readonly bool IsFirstChannelExtDefault = false;
    public static readonly int PacketSizeDefault = 64;
    public static readonly int MinimumPumpPowerDefault = 50;

    public bool? IsFirstChannelExt { get; set; }

    public int? PacketSize { get; set; }

    public int? MinimumPumpPower { get; set; }
}
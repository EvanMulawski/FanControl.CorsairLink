namespace CorsairLink.Devices;

public class HydroAsetekProDeviceOptions
{
    public static readonly bool OverrideSafetyProfileDefault = false;

    public uint FanChannelCount { get; set; }

    public bool? OverrideSafetyProfile { get; set; }
}
namespace CorsairLink.Devices;

public class HydroPlatinumDeviceOptions
{
    public int FanChannelCount { get; set; }

    public RgbColor? DirectLightingDefaultColor { get; set; }

    public int? DirectLightingDefaultBrightness { get; set; }

    public bool? DisableDirectLightingAfterReset { get; set; }
}
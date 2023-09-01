using System;

namespace CorsairLink.SiUsbXpress.Driver;

public class AsetekSiUsbXpressDevice : SiUsbXpressDevice
{
    private readonly SiUsbXpressDeviceOptions _deviceOptions = new()
    {
        ReadBufferSize = 32,
        ReadTimeout = TimeSpan.FromMilliseconds(500),
        WriteTimeout = TimeSpan.FromMilliseconds(500),
    };

    public AsetekSiUsbXpressDevice(SiUsbXpressDeviceInfo deviceInfo)
        : base(deviceInfo)
    {
    }

    protected override SiUsbXpressDeviceOptions DeviceOptions => _deviceOptions;

    public override void Open()
    {
        base.Open();

        _ = SiUsbXpressDriver.SI_SetTimeouts((uint)DeviceOptions.ReadTimeout.TotalMilliseconds, (uint)DeviceOptions.ReadTimeout.TotalMilliseconds);
        FlushBuffers();
    }
}

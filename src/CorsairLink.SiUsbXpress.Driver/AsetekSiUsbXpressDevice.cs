namespace CorsairLink.SiUsbXpress.Driver;

public class AsetekSiUsbXpressDevice : SiUsbXpressDevice
{
    private const uint DEFAULT_TIMEOUT = 500U;
    private const uint READ_BUFFER_SIZE = 32;

    public AsetekSiUsbXpressDevice(SiUsbXpressDeviceInfo deviceInfo)
        : base(deviceInfo)
    {
    }

    protected override uint ReadBufferSize { get; } = READ_BUFFER_SIZE;

    public override void Open()
    {
        base.Open();

        _ = SiUsbXpressDriver.SI_SetTimeouts(DEFAULT_TIMEOUT, DEFAULT_TIMEOUT);
        FlushBuffers();
    }
}

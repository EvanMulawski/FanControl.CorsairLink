using System.Collections.Generic;

namespace CorsairLink.SiUsbXpress.Driver;

public sealed class SiUsbXpressDeviceEnumerator : ISiUsbXpressDeviceEnumerator
{
    public SiUsbXpressDeviceEnumerator()
    {
    }

    public IReadOnlyCollection<SiUsbXpressDeviceInfo> Enumerate()
    {
        return SiUsbXpressDriverHelper.EnumerateDevices();
    }
}

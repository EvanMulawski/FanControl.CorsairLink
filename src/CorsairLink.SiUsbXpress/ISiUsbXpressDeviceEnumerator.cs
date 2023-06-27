using System.Collections.Generic;

namespace CorsairLink.SiUsbXpress;

public interface ISiUsbXpressDeviceEnumerator
{
    IReadOnlyCollection<SiUsbXpressDeviceInfo> Enumerate();
}

using System;

namespace CorsairLink.SiUsbXpress.Driver;

[Serializable]
public class SiUsbXpressDeviceException : Exception
{
    public SiUsbXpressDeviceException() : base()
    {
    }

    public SiUsbXpressDeviceException(string message) : base(message)
    {
    }
}

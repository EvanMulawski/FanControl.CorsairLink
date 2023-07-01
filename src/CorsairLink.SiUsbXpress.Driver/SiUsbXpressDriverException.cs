using System;

namespace CorsairLink.SiUsbXpress.Driver;

[Serializable]
public class SiUsbXpressDriverException : SiUsbXpressException
{
    public SiUsbXpressDriverException(SiUsbXpressDriver.SI_STATUS driverStatus)
        : base("Driver operation failed.")
    {
        Data[nameof(driverStatus)] = driverStatus;
    }
}

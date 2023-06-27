using System;

namespace CorsairLink.SiUsbXpress.Driver;

[Serializable]
public class SiUsbXpressDeviceOperationException : SiUsbXpressDeviceException
{
    public SiUsbXpressDeviceOperationException(SiUsbXpressDriver.SI_STATUS code) => ErrorCode = code;

    public SiUsbXpressDriver.SI_STATUS ErrorCode { get; }
}

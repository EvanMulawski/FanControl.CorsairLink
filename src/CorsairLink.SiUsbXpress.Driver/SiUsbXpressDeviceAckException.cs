using System;

namespace CorsairLink.SiUsbXpress.Driver;

[Serializable]
public class SiUsbXpressDeviceAckException : SiUsbXpressDeviceException
{
    public SiUsbXpressDeviceAckException(AckStatus ackStatus) => AckStatus = ackStatus;

    public AckStatus AckStatus { get; }
}
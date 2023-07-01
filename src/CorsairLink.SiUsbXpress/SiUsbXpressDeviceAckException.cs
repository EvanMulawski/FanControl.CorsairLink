using System;

namespace CorsairLink.SiUsbXpress;

[Serializable]
public class SiUsbXpressDeviceAckException : SiUsbXpressException
{
    public SiUsbXpressDeviceAckException(AckStatus ackStatus)
        : base("Ack unsuccessful.")
    {
        Data[nameof(ackStatus)] = ackStatus;
    }
}
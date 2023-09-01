using System;

namespace CorsairLink.SiUsbXpress;

public class SiUsbXpressDeviceOptions
{
    public uint ReadBufferSize { get; set; }
    public TimeSpan ReadTimeout { get; set; }
    public TimeSpan WriteTimeout { get; set; }
}

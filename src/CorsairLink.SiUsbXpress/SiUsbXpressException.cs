using System;

namespace CorsairLink.SiUsbXpress;

[Serializable]
public class SiUsbXpressException : Exception
{
    public SiUsbXpressException() : base()
    {
    }

    public SiUsbXpressException(string message) : base(message)
    {
    }
}

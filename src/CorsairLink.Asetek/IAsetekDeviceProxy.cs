using System;

namespace CorsairLink.Asetek;

public interface IAsetekDeviceProxy
{
    AsetekDeviceInfo GetDeviceInfo();
    (bool Opened, Exception? Exception) Open();
    void Close();
    byte[] WriteAndRead(byte[] buffer);
}

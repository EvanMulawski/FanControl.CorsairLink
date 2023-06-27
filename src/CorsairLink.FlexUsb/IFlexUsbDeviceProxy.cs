using System;

namespace CorsairLink.FlexUsb;

public interface IFlexUsbDeviceProxy
{
    FlexUsbDeviceInfo GetDeviceInfo();
    (bool Opened, Exception? Exception) Open();
    void Close();
    void Write(CommandCode command, byte[] buffer);
    byte[] Read(CommandCode command, int length);
}

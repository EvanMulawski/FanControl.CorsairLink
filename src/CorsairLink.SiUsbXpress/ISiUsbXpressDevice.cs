using System;
using System.Runtime.InteropServices;

namespace CorsairLink.SiUsbXpress;

public interface ISiUsbXpressDevice : IDisposable
{
    SafeHandle? DeviceHandle { get; }
    SiUsbXpressDeviceInfo DeviceInfo { get; }
    bool IsOpen { get; }

    void Close();
    void FlushBuffers();
    string GetProductString(ProductString productString);
    void Open();
    byte[] Read();
    void Write(byte[] buffer);
    byte[] WriteAndRead(byte[] buffer);
    void WriteAndValidate(byte[] buffer);
    void WriteWhileBusy(byte[] buffer);
}

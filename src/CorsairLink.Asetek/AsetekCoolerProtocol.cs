using CorsairLink.SiUsbXpress;
using System;

namespace CorsairLink.Asetek;

public class AsetekCoolerProtocol : IAsetekDeviceProxy, IDisposable
{
    private bool _disposedValue;

    public AsetekCoolerProtocol(ISiUsbXpressDevice device)
    {
        Device = device;
    }

    protected ISiUsbXpressDevice Device { get; }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Device?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public virtual AsetekDeviceInfo GetDeviceInfo()
    {
        return new AsetekDeviceInfo(
            Device.DeviceInfo.DevicePath,
            Device.DeviceInfo.VendorId,
            Device.DeviceInfo.ProductId,
            Device.DeviceInfo.Name,
            Device.DeviceInfo.SerialNumber);
    }

    public virtual (bool Opened, Exception? Exception) Open()
    {
        try
        {
            Device.Open();
            return (true, default);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    public virtual void Close()
    {
        Device.Close();
    }

    public virtual byte[] WriteAndRead(byte[] buffer)
    {
        return Device.WriteAndRead(buffer);
    }
}

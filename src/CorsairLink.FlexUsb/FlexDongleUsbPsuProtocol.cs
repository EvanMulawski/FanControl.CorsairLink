using CorsairLink.SiUsbXpress;
using System;

namespace CorsairLink.FlexUsb;

public class FlexDongleUsbPsuProtocol : IFlexUsbDeviceProxy, IDisposable
{
    private bool _disposedValue;

    public FlexDongleUsbPsuProtocol(ISiUsbXpressDevice device)
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

    public virtual FlexUsbDeviceInfo GetDeviceInfo()
    {
        return new FlexUsbDeviceInfo(
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
            WriteSMBusSettings();
            return (true, default);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    private void WriteSMBusSettings()
    {
        Device.WriteAndRead(PacketGenerator.CreateWriteSMBusSettingsBuffer());
    }

    public virtual void Close()
    {
        Device.Close();
    }

    public virtual void Write(CommandCode command, byte[] buffer)
    {
        Device.WriteAndValidate(PacketGenerator.CreateWriteSMBusCommandForWriteBuffer(command, buffer));
    }

    public virtual byte[] Read(CommandCode command, int length)
    {
        Device.WriteAndValidate(PacketGenerator.CreateWriteSMBusCommandForReadBuffer(command, length));
        return Device.WriteAndRead(PacketGenerator.CreateReadMemoryBuffer(length));
    }
}

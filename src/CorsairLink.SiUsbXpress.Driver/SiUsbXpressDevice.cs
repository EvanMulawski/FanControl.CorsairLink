using Microsoft.Win32.SafeHandles;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace CorsairLink.SiUsbXpress.Driver;

public abstract class SiUsbXpressDevice : ISiUsbXpressDevice
{
    protected uint _deviceNumber = 0;

    public SiUsbXpressDevice(SiUsbXpressDeviceInfo deviceInfo)
    {
        DeviceInfo = deviceInfo;
    }

    public SafeHandle? DeviceHandle { get; private set; }

    public SiUsbXpressDeviceInfo DeviceInfo { get; }

    protected abstract uint ReadBufferSize { get; }

    public bool IsOpen => DeviceHandle != null && !DeviceHandle.IsInvalid && !DeviceHandle.IsClosed;

    public virtual void Dispose() => Close();

    public virtual void Close()
    {
        if (!IsOpen)
            return;

        try
        {
            DeviceHandle?.Dispose();
            DeviceHandle = null;
        }
        catch
        {
            // ignore
        }
    }

    public virtual void Open()
    {
        if (IsOpen)
            return;

        var deviceNumber = SiUsbXpressDriverHelper.FindDevice(DeviceInfo);

        if (!deviceNumber.HasValue)
            throw new SiUsbXpressDriverException(SiUsbXpressDriver.SI_STATUS.SI_DEVICE_NOT_FOUND);

        var handle = IntPtr.Zero;
        SiUsbXpressDriver.SI_STATUS code = SiUsbXpressDriver.SI_Open((uint)deviceNumber, ref handle);

        if (!code.IsSuccess())
            throw new SiUsbXpressDriverException(code);

        _deviceNumber = (uint)deviceNumber.Value;
        DeviceHandle = new SafeFileHandle(handle, true);
    }

    protected void WriteInternal(byte[] data)
    {
        uint lpdwBytesWritten = 0;
        SiUsbXpressDriver.SI_STATUS code = SiUsbXpressDriver.SI_Write(DeviceHandle!.DangerousGetHandle(), data, (uint)data.Length, ref lpdwBytesWritten, IntPtr.Zero);
        if (code.IsError() || lpdwBytesWritten != data.Length)
            throw new SiUsbXpressDriverException(code);
    }

    protected byte[] ReadInternal()
    {
        byte[] buffer = new byte[ReadBufferSize];
        uint lpdwBytesReturned = 0;
        SiUsbXpressDriver.SI_STATUS code = SiUsbXpressDriver.SI_Read(DeviceHandle!.DangerousGetHandle(), buffer, (uint)buffer.Length, ref lpdwBytesReturned, IntPtr.Zero);
        if (code.IsError())
            throw new SiUsbXpressDriverException(code);
        return buffer.Take((int)lpdwBytesReturned).ToArray();
    }

    protected virtual byte[] GetWriteBuffer(byte[] originalBuffer)
    {
        return originalBuffer;
    }

    public virtual void Write(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        WriteInternal(GetWriteBuffer(buffer));
    }

    protected virtual byte[] GetReadBuffer(byte[] originalBuffer)
    {
        return originalBuffer;
    }

    public virtual byte[] Read()
    {
        ThrowIfDeviceNotReady();

        return GetReadBuffer(ReadInternal());
    }

    public virtual byte[] WriteAndRead(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        Write(buffer);
        return Read();
    }

    public virtual void WriteAndValidate(byte[] buffer)
    {
        throw new NotSupportedException();
    }

    public virtual void WriteWhileBusy(byte[] buffer)
    {
        throw new NotSupportedException();
    }

    public virtual void FlushBuffers()
    {
        ThrowIfDeviceNotReady();

        _ = SiUsbXpressDriver.SI_FlushBuffers(DeviceHandle!.DangerousGetHandle(), 0x01, 0x01);
    }

    protected void ThrowIfDeviceNotReady()
    {
        if (!IsOpen)
            throw new SiUsbXpressException("Device not ready.");
    }

    public virtual string GetProductString(ProductString productString)
    {
        byte[] data = new byte[SiUsbXpressDriver.SI_MAX_DEVICE_STRLEN];
        return SiUsbXpressDriver.SI_GetProductString(_deviceNumber, data, (byte)productString).IsError()
            ? string.Empty
            : Utils.ParseString(data);
    }
}

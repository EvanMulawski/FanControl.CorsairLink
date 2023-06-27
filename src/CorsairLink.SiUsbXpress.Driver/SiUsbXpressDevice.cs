using Microsoft.Win32.SafeHandles;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace CorsairLink.SiUsbXpress.Driver;

public sealed class SiUsbXpressDevice : ISiUsbXpressDevice
{
    private const uint MAX_BAUD_RATE = 115200U;
    private const uint DEFAULT_TIMEOUT = 200U;
    private const uint READ_BUFFER_SIZE = 16;

    private uint _deviceNumber = 0;

    public SiUsbXpressDevice(SiUsbXpressDeviceInfo deviceInfo)
    {
        DeviceInfo = deviceInfo;
    }

    public SafeHandle? DeviceHandle { get; private set; }

    public SiUsbXpressDeviceInfo DeviceInfo { get; }

    public bool IsOpen => DeviceHandle != null && !DeviceHandle.IsInvalid && !DeviceHandle.IsClosed;

    public void Dispose() => Close();

    public void Close()
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

    public void Open()
    {
        if (IsOpen)
            return;

        var deviceNumber = SiUsbXpressDriverHelper.FindDevice(DeviceInfo);

        if (!deviceNumber.HasValue)
            throw new SiUsbXpressDeviceOperationException(SiUsbXpressDriver.SI_STATUS.SI_DEVICE_NOT_FOUND);

        var handle = IntPtr.Zero;
        SiUsbXpressDriver.SI_STATUS code = SiUsbXpressDriver.SI_Open((uint)deviceNumber, ref handle);

        if (code.IsSuccess())
        {
            _deviceNumber = (uint)deviceNumber.Value;
            DeviceHandle = new SafeFileHandle(handle, true);
        }

        if (!code.IsSuccess())
            throw new SiUsbXpressDeviceOperationException(code);

        _ = SiUsbXpressDriver.SI_SetTimeouts(DEFAULT_TIMEOUT, DEFAULT_TIMEOUT);
        FlushBuffers();
        _ = SiUsbXpressDriver.SI_SetBaudRate(DeviceHandle!.DangerousGetHandle(), MAX_BAUD_RATE);
    }

    private static void WriteDataImpl(SafeHandle handle, byte[] data)
    {
        uint lpdwBytesWritten = 0;
        SiUsbXpressDriver.SI_STATUS code = SiUsbXpressDriver.SI_Write(handle.DangerousGetHandle(), data, (uint)data.Length, ref lpdwBytesWritten, IntPtr.Zero);
        if (code.IsError() || lpdwBytesWritten != data.Length)
            throw new SiUsbXpressDeviceOperationException(code);
    }

    private static byte[] ReadDataImpl(SafeHandle handle)
    {
        byte[] buffer = new byte[READ_BUFFER_SIZE];
        uint lpdwBytesReturned = 0;
        SiUsbXpressDriver.SI_STATUS code = SiUsbXpressDriver.SI_Read(handle.DangerousGetHandle(), buffer, (uint)buffer.Length, ref lpdwBytesReturned, IntPtr.Zero);
        if (code.IsError())
            throw new SiUsbXpressDeviceOperationException(code);
        return buffer.Take((int)lpdwBytesReturned).ToArray();
    }

    public void Write(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        byte[] encodedData = EncodingHelper.EncodeData(buffer);
        WriteDataImpl(DeviceHandle!, encodedData);
    }

    public byte[] Read()
    {
        ThrowIfDeviceNotReady();

        byte[] encodedData = ReadDataImpl(DeviceHandle!);
        return !EncodingHelper.HasError(encodedData)
            ? EncodingHelper.DecodeData(encodedData)
            : throw new SiUsbXpressDeviceException("Failed to read - data error.");
    }

    public AckStatus ReadAckStatus()
        => AckParser.Parse(Read());

    public void WriteAndValidate(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        Write(buffer);
        AckStatus ackStatus = ReadAckStatus();
        if (ackStatus != AckStatus.Ok)
            throw new SiUsbXpressDeviceAckException(ackStatus);
    }

    public void WriteWhileBusy(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        AckStatus ackStatus;
        do
        {
            Write(buffer);
            ackStatus = ReadAckStatus();
        }
        while (ackStatus == AckStatus.Busy);
        if (ackStatus != AckStatus.Ok)
            throw new SiUsbXpressDeviceAckException(ackStatus);
    }

    public byte[] WriteAndRead(byte[] buffer)
    {
        ThrowIfDeviceNotReady();

        Write(buffer);
        return Read();
    }

    public void FlushBuffers()
    {
        ThrowIfDeviceNotReady();

        _ = SiUsbXpressDriver.SI_FlushBuffers(DeviceHandle!.DangerousGetHandle(), 0x01, 0x01);
    }

    private void ThrowIfDeviceNotReady()
    {
        if (!IsOpen)
            throw new SiUsbXpressDeviceException("Device not ready.");
    }

    public string GetProductString(ProductString productString)
    {
        byte[] data = new byte[SiUsbXpressDriver.SI_MAX_DEVICE_STRLEN];
        return SiUsbXpressDriver.SI_GetProductString(_deviceNumber, data, (byte)productString).IsError()
            ? string.Empty
            : Utils.ParseString(data);
    }
}

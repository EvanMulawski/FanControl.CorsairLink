using HidSharp;
using System.Security.Cryptography;
using System.Text;

namespace CorsairLink;

internal class HidSharpDeviceProxy : IHidDeviceProxy
{
    private readonly HidDevice _device;
    private HidStream? _stream;

    public HidSharpDeviceProxy(HidDevice device)
    {
        _device = device;
    }

    public void Close()
    {
        _stream?.Dispose();
        _stream = null;
    }

    public HidDeviceInfo GetDeviceInfo()
    {
        return new HidDeviceInfo(
            _device.DevicePath,
            _device.VendorID,
            _device.ProductID,
            _device.GetProductName(),
            GetSerialNumber());
    }

    private string GetSerialNumber()
    {
        try
        {
            return _device.GetSerialNumber();
        }
        catch
        {
            // some devices do not support serial numbers
            // HidSharp.Exceptions.DeviceIOException: Failed to get info.
            // hash the device path instead

            var hash = MD5.Create().ComputeHash(Encoding.Default.GetBytes(_device.DevicePath));
            return hash.ToHexString();
        }
    }

    public (bool Opened, Exception? Exception) Open()
    {
        Close();

        try
        {
            var opened = _device.TryOpen(out _stream);
            return (opened, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    public void Read(byte[] buffer)
    {
        ThrowIfNotReady();
        
        _stream?.Read(buffer, 0, buffer.Length);
    }

    public void Write(byte[] buffer)
    {
        ThrowIfNotReady();

        ClearEnqueuedReports();
        _stream?.Write(buffer, 0, buffer.Length);
    }

    public void ClearEnqueuedReports()
    {
        ThrowIfNotReady();

        var originalReadTimeout = _stream!.ReadTimeout;
        _stream.ReadTimeout = 1;

        try
        {
            while (true)
            {
                _ = _stream.Read();
            }
        }
        catch (TimeoutException)
        {
            // cleared!
        }
        finally
        {
            _stream.ReadTimeout = originalReadTimeout;
        }
    }

    private void ThrowIfNotReady()
    {
        bool @throw;
        try
        {
            @throw = _stream is null;
        }
        catch (ObjectDisposedException)
        {
            @throw = true;
        }

        if (@throw)
        {
            throw new InvalidOperationException("The device is not ready.");
        }
    }
}

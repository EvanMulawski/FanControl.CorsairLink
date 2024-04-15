using HidSharp;
using System.ComponentModel;

namespace CorsairLink.Hid;

public sealed class HidSharpDeviceProxy : IHidDeviceProxy
{
    private const int DEFAULT_READ_TIMEOUT_MS = 500;
    private const int DEFAULT_WRITE_TIMEOUT_MS = 500;

    private readonly HidDevice _device;
    private HidStream? _stream;
    private Action? _reconnectAction;

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
            _device.GetProductNameOrDefault(),
            _device.GetSerialNumberOrDefault());
    }

    public (bool Opened, Exception? Exception) Open()
    {
        Close();

        try
        {
            var opened = _device.TryOpen(out _stream);
            if (opened)
            {
                _stream.ReadTimeout = DEFAULT_READ_TIMEOUT_MS;
                _stream.WriteTimeout = DEFAULT_WRITE_TIMEOUT_MS;
            }
            return (opened, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    public void OnReconnect(Action? reconnectAction)
    {
        _reconnectAction = reconnectAction;
    }

    private void ExecuteWithReconnect(Action<HidStream, byte[]> streamAction, ref HidStream stream, byte[] buffer)
    {
        try
        {
            streamAction(stream, buffer);
        }
        catch (IOException ex) when (ex.InnerException is Win32Exception w32 && w32.ErrorCode == 0x0000048F)
        {
            var reopenResult = Open();
            if (!reopenResult.Opened)
            {
                throw;
            }

            _reconnectAction?.Invoke();
            streamAction(stream, buffer);
        }
    }

    public void Read(byte[] buffer)
    {
        ThrowIfNotReady();
        ExecuteWithReconnect(ReadInternal, ref _stream!, buffer);
    }

    private static void ReadInternal(HidStream stream, byte[] buffer)
    {
        stream.Read(buffer, 0, buffer.Length);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Signature needed for generic execution/retry wrapper")]
    private static void ReadAnyInternal(HidStream stream, byte[] buffer)
    {
        _ = stream.Read();
    }

    public void Write(byte[] buffer)
    {
        ThrowIfNotReady();
        ClearEnqueuedReports();
        ExecuteWithReconnect(WriteInternal, ref _stream!, buffer);
    }

    public void WriteDirect(byte[] buffer)
    {
        ThrowIfNotReady();
        ExecuteWithReconnect(WriteInternal, ref _stream!, buffer);
    }

    private static void WriteInternal(HidStream stream, byte[] buffer)
    {
        stream.Write(buffer, 0, buffer.Length);
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
                ExecuteWithReconnect(ReadAnyInternal, ref _stream!, null!);
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

    public void GetFeature(byte[] buffer)
    {
        ThrowIfNotReady();
        ExecuteWithReconnect(GetFeatureInternal, ref _stream!, buffer);
    }

    private static void GetFeatureInternal(HidStream stream, byte[] buffer)
    {
        stream.GetFeature(buffer, 0, buffer.Length);
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

using HidSharp;
using System.Security.Cryptography;
using System.Text;

namespace CorsairLink;

public static class HidSharpDeviceExtensions
{
    public static string GetProductNameOrDefault(this HidDevice device)
    {
        try
        {
            return device.GetProductName();
        }
        catch
        {
            // some devices do not support a product name
            // HidSharp.Exceptions.DeviceIOException: Failed to get info.
            return "NO_NAME";
        }
    }

    public static string GetSerialNumberOrDefault(this HidDevice device)
    {
        try
        {
            return device.GetSerialNumber();
        }
        catch
        {
            // some devices do not support serial numbers
            // HidSharp.Exceptions.DeviceIOException: Failed to get info.
            // hash the device path instead

            var hash = MD5.Create().ComputeHash(Encoding.Default.GetBytes(device.DevicePath));
            return hash.ToHexString();
        }
    }
}

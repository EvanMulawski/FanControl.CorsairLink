using System;
using System.Linq;

namespace CorsairLink.FlexUsb;

public static class FlexUsbDeviceProxyExtensions
{
    private const int MAX_RETRY_COUNT = 5;

    public static float ReadNumber(this IFlexUsbDeviceProxy device, CommandCode command)
    {
        byte[] data = device.Read(command, 2);
        return Utils.FromLinear11(data);
    }

    public static string ReadString(this IFlexUsbDeviceProxy device, CommandCode command)
    {
        byte[] data = device.Read(command, 7);
        return Utils.ParseString(data);
    }

    public static byte ReadByte(this IFlexUsbDeviceProxy device, CommandCode command)
    {
        byte[] data = device.Read(command, 1);
        return data[0];
    }

    public static bool WriteByte(this IFlexUsbDeviceProxy device, CommandCode command, byte value)
    {
        return WriteWithRetry(device, command, new byte[1]
        {
            value
        });
    }

    public static bool WriteWithRetry(this IFlexUsbDeviceProxy device, CommandCode command, byte[] data)
    {
        bool success = false;
        int triesCount = 0;
        while (!success)
        {
            device.Write(command, data);
            byte[] readData = device.Read(command, data.Length);
            success = readData.SequenceEqual(data);

            if (++triesCount > MAX_RETRY_COUNT)
                break;
        }
        return success;
    }
}

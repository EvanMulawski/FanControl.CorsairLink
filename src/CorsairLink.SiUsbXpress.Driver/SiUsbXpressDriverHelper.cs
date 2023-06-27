using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CorsairLink.SiUsbXpress.Driver;

public static class SiUsbXpressDriverHelper
{
    public static string GetProductString(uint dwDeviceNum, byte dwFlag)
    {
        byte[] data = new byte[SiUsbXpressDriver.SI_MAX_DEVICE_STRLEN];
        return SiUsbXpressDriver.SI_GetProductString(dwDeviceNum, data, dwFlag).IsError()
            ? string.Empty
            : Utils.ParseString(data);
    }

    public static string GetDeviceVendorId(uint dwDeviceNum)
        => GetProductString(dwDeviceNum, SiUsbXpressDriver.SI_RETURN_VID);

    public static string GetDeviceProductId(uint dwDeviceNum)
        => GetProductString(dwDeviceNum, SiUsbXpressDriver.SI_RETURN_PID);

    public static string GetDeviceName(uint dwDeviceNum)
        => GetProductString(dwDeviceNum, SiUsbXpressDriver.SI_RETURN_DESCRIPTION);

    public static string GetDevicePath(uint dwDeviceNum)
        => GetProductString(dwDeviceNum, SiUsbXpressDriver.SI_RETURN_LINK_NAME);

    public static string GetDeviceSerialNumber(uint dwDeviceNum)
        => GetProductString(dwDeviceNum, SiUsbXpressDriver.SI_RETURN_SERIAL_NUMBER);

    public static void FlushBuffers(SafeHandle handle)
    {
        _ = SiUsbXpressDriver.SI_FlushBuffers(handle.DangerousGetHandle(), 0x01, 0x01);
    }

    public static IReadOnlyList<SiUsbXpressDeviceInfo> EnumerateDevices()
    {
        var collection = new List<SiUsbXpressDeviceInfo>();
        uint lpdwNumDevices = 0;

        _ = SiUsbXpressDriver.SI_GetNumDevices(ref lpdwNumDevices);
        for (uint dwDeviceNum = 0; dwDeviceNum < lpdwNumDevices; ++dwDeviceNum)
        {
            SiUsbXpressDeviceInfo deviceInfo = new(
                GetDevicePath(dwDeviceNum),
                HardwareIdToInt(GetDeviceVendorId(dwDeviceNum)),
                HardwareIdToInt(GetDeviceProductId(dwDeviceNum)),
                GetDeviceSerialNumber(dwDeviceNum),
                GetDeviceName(dwDeviceNum));

            collection.Add(deviceInfo);
        }

        return collection;
    }

    private static int HardwareIdToInt(string hardwareId)
    {
        if (hardwareId.Length != 4)
        {
            throw new ArgumentException("Invalid hardware ID. Must be 4 characters long.", nameof(hardwareId));
        }

        if (!int.TryParse(hardwareId, System.Globalization.NumberStyles.HexNumber, null, out int result))
        {
            throw new ArgumentException("Invalid hardware ID format.", nameof(hardwareId));
        }

        return result;
    }

    public static int? FindDevice(SiUsbXpressDeviceInfo deviceInfo)
    {
        var devices = EnumerateDevices();

        for (var num = 0; num < devices.Count; num++)
        {
            if (deviceInfo.Equals(devices[num]))
            {
                return num;
            }
        }

        return default;
    }

    public static bool IsError(this SiUsbXpressDriver.SI_STATUS code)
        => code != SiUsbXpressDriver.SI_STATUS.SI_SUCCESS;

    public static bool IsSuccess(this SiUsbXpressDriver.SI_STATUS code)
        => code == SiUsbXpressDriver.SI_STATUS.SI_SUCCESS;
}

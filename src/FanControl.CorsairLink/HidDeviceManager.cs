using CorsairLink.Devices;
using CorsairLink.Hid;
using HidSharp;
using System.Text;

namespace CorsairLink;

public static class HidDeviceManager
{
    public static IReadOnlyCollection<IDevice> GetSupportedDevices(IDeviceGuardManager deviceGuardManager, ILogger logger)
    {
        var corsairDevices = DeviceList.Local
            .GetHidDevices(vendorID: HardwareIds.CorsairVendorId)
            .ToList();
        logger.LogDevices(corsairDevices, "Corsair HID device(s)");

        var supportedProductIds = HardwareIds.GetSupportedProductIds();

        var supportedDevices = corsairDevices
            .Where(x => supportedProductIds.Contains(x.ProductID) && x.GetMaxOutputReportLength() > 0)
            .ToList();
        logger.LogDevices(supportedDevices, "supported Corsair HID device(s)");

        var collection = new List<IDevice>();

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderPro)
            .Select(x => new CommanderProDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderCore)
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = false }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderCoreWithDesignatedPump)
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = true }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroPlatinum2Fan)
            .Select(x => new HydroPlatinumDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new HydroPlatinumDeviceOptions { FanChannelCount = 2 }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroPlatinum3Fan)
            .Select(x => new HydroPlatinumDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new HydroPlatinumDeviceOptions { FanChannelCount = 3 }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CoolitFamily)
            .Select(x => new CoolitDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HidPowerSupplyUnits)
            .Select(x => new HidPsuDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        return collection;
    }

    private static IEnumerable<HidDevice> InDeviceDriverGroup(this IEnumerable<HidDevice> devices, IEnumerable<int> deviceDriverGroup)
    {
        return devices.Join(deviceDriverGroup, d => d.ProductID, g => g, (d, _) => d);
    }

    private static void LogDevices(this ILogger logger, IReadOnlyCollection<HidDevice> devices, string description)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Found {devices.Count} {description}");
        foreach (var device in devices)
        {
            sb.AppendLine($"  name={device.GetProductNameOrDefault()}, devicePath={device.DevicePath}");
        }
        logger.Info("HID Device Manager", sb.ToString());
    }
}

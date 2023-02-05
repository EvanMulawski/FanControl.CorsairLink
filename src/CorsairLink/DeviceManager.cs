using HidSharp;
using System.Text;

namespace CorsairLink;

public static class DeviceManager
{
    public static SupportedDeviceCollection GetSupportedDevices(IDeviceGuardManager deviceGuardManager, ILogger? logger)
    {
        var corsairDevices = DeviceList.Local
            .GetHidDevices(vendorID: HardwareIds.CorsairVendorId)
            .ToList();
        logger?.LogDevices(corsairDevices, "Corsair device(s)");

        var supportedDevices = corsairDevices
            .Where(x => HardwareIds.SupportedProductIds.Contains(x.ProductID) && x.GetMaxOutputReportLength() > 0)
            .ToList();
        logger?.LogDevices(supportedDevices, "supported Corsair device(s)");

        var supportedDevicesByProductId = supportedDevices
            .ToLookup(x => x.ProductID);

        var collection = new SupportedDeviceCollection();

        collection.CommanderProDevices
            .AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderPro)
                .Select(x => new CommanderProDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderCore)
                .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = false }, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderCoreWithDesignatedPump)
                .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = true }, logger)));

        collection.HydroDevices
            .AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.Hydro2Fan)
                .Select(x => new HydroDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new HydroDeviceOptions { FanChannelCount = 2 }, logger)));

        collection.HydroDevices
            .AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.Hydro3Fan)
                .Select(x => new HydroDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new HydroDeviceOptions { FanChannelCount = 3 }, logger)));

        return collection;
    }

    private static IEnumerable<HidDevice> InDeviceDriverGroup(this IEnumerable<HidDevice> devices, IEnumerable<int> deviceDriverGroup)
    {
        return devices.Join(deviceDriverGroup, d => d.ProductID, g => g, (d, _) => d);
    }

    private static void LogDevices(this ILogger? logger, IReadOnlyCollection<HidDevice> devices, string description)
    {
        if (logger is null)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"DeviceManager: Found {devices.Count} {description}");
        foreach (var device in devices)
        {
            sb.AppendLine($"  name={device.GetProductName()}, devicePath={device.DevicePath}");
        }
        logger.Log(sb.ToString());
    }
}

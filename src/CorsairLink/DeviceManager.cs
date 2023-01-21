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
            .AddRange(supportedDevicesByProductId[HardwareIds.CorsairCommanderProProductId]
            .Select(x => new CommanderProDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.CommanderProDevices
            .AddRange(supportedDevicesByProductId[HardwareIds.CorsairObsidian1000DCommanderProProductId]
            .Select(x => new CommanderProDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevicesByProductId[HardwareIds.CorsairCommanderCoreXTProductId]
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = false }, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevicesByProductId[HardwareIds.CorsairCommanderCoreProductId]
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = true }, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevicesByProductId[HardwareIds.CorsairCommanderSTProductId]
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions { IsFirstChannelExt = true }, logger)));

        return collection;
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

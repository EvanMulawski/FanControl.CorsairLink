using CorsairLink.Devices.FlexUsbPsu;
using CorsairLink.FlexUsb;
using CorsairLink.SiUsbXpress;
using CorsairLink.SiUsbXpress.Driver;
using System.Text;

namespace CorsairLink;

public static class SiUsbXpressDeviceManager
{
    public static IReadOnlyCollection<IDevice> GetSupportedDevices(IDeviceGuardManager deviceGuardManager, ILogger logger)
    {
        var corsairDevices = new SiUsbXpressDeviceEnumerator().Enumerate()
            .Where(x => x.VendorId == HardwareIds.CorsairVendorId)
            .ToList();
        logger.LogDevices(corsairDevices, "Corsair SiUsbXpress device(s)");

        var supportedProductIds = HardwareIds.GetSupportedProductIds();

        var supportedDevices = corsairDevices
            .Where(x => supportedProductIds.Contains(x.ProductId))
            .ToList();
        logger.LogDevices(supportedDevices, "supported Corsair SiUsbXpress device(s)");

        var collection = new List<IDevice>();

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.FlexDongleUsbPowerSupplyUnits)
            .Select(x => new FlexUsbPsuDevice(new FlexDongleUsbPsuProtocol(new SiUsbXpressDevice(x)), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.FlexModernUsbPowerSupplyUnits)
            .Select(x => new FlexUsbPsuDevice(new ModernPsuProtocol(new SiUsbXpressDevice(x)), deviceGuardManager, logger)));

        return collection;
    }

    private static IEnumerable<SiUsbXpressDeviceInfo> InDeviceDriverGroup(this IEnumerable<SiUsbXpressDeviceInfo> devices, IEnumerable<int> deviceDriverGroup)
    {
        return devices.Join(deviceDriverGroup, d => d.ProductId, g => g, (d, _) => d);
    }

    private static void LogDevices(this ILogger logger, IReadOnlyCollection<SiUsbXpressDeviceInfo> devices, string description)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Found {devices.Count} {description}");
        foreach (var device in devices)
        {
            sb.AppendLine($"  name={device.Name}, devicePath={device.DevicePath}");
        }
        logger.Info("SiUsbXpress Device Manager", sb.ToString());
    }
}

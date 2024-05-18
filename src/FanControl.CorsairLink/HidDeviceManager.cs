using CorsairLink.Devices;
using CorsairLink.Devices.HidCooling;
using CorsairLink.Devices.ICueLink;
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

        var globalMinimumPumpPowerValue = Utils.GetEnvironmentInt32("FANCONTROL_CORSAIRLINK_MIN_PUMP_DUTY");
        var directLightingDefaultColorValue = Utils.GetEnvironmentString("FANCONTROL_CORSAIRLINK_DIRECT_LIGHTING_DEFAULT_RGB");
        _ = RgbColor.TryParse(directLightingDefaultColorValue, out var directLightingDefaultColor);
        var directLightingDefaultBrightnessValue = Utils.GetEnvironmentInt32("FANCONTROL_CORSAIRLINK_DIRECT_LIGHTING_DEFAULT_BRIGHTNESS");
        var directLightingDisableAfterReset = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_DIRECT_LIGHTING_DISABLE_AFTER_RESET");

        var collection = new List<IDevice>();

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderPro)
            .Select(x => new CommanderProDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.ICueLinkHub)
            .Select(x => new ICueLinkHubDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new ICueLinkHubDeviceOptions
            {
                MinimumPumpPower = globalMinimumPumpPowerValue,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderCore)
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions
            {
                IsFirstChannelExt = false,
                MinimumPumpPower = globalMinimumPumpPowerValue,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CommanderCoreWithDesignatedPump)
            .Select(x => new CommanderCoreDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new CommanderCoreDeviceOptions
            {
                IsFirstChannelExt = true,
                PacketSize = x.ProductID == HardwareIds.CorsairCommanderCoreProductId ? 96 : null,
                MinimumPumpPower = globalMinimumPumpPowerValue,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroPlatinum2Fan)
            .Select(x => new HydroPlatinumDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new HydroPlatinumDeviceOptions
            {
                FanChannelCount = 2,
                DirectLightingDefaultBrightness = directLightingDefaultBrightnessValue,
                DirectLightingDefaultColor = directLightingDefaultColor,
                DisableDirectLightingAfterReset = directLightingDisableAfterReset,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroPlatinum3Fan)
            .Select(x => new HydroPlatinumDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new HydroPlatinumDeviceOptions
            {
                FanChannelCount = 3,
                DirectLightingDefaultBrightness = directLightingDefaultBrightnessValue,
                DirectLightingDefaultColor = directLightingDefaultColor,
                DisableDirectLightingAfterReset = directLightingDisableAfterReset,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.One)
            .Select(x => new OneComputerDevice(new HidSharpDeviceProxy(x), deviceGuardManager, new OneComputerDeviceOptions
            {
                DirectLightingDefaultBrightness = directLightingDefaultBrightnessValue,
                DirectLightingDefaultColor = directLightingDefaultColor,
                DisableDirectLightingAfterReset = directLightingDisableAfterReset,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.CoolitFamily)
            .Select(x => new CoolitDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HidPowerSupplyUnits)
            .Select(x => new HidPsuDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.Xc7)
            .Select(x => new Xc7LcdWaterBlockDevice(new HidSharpDeviceProxy(x), deviceGuardManager, logger)));

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

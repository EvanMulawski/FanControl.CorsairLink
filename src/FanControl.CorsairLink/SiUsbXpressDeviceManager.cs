using CorsairLink.Asetek;
using CorsairLink.Devices;
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

        var psuZeroRpmDutyThresholdValue = Utils.GetEnvironmentInt32("FANCONTROL_CORSAIRLINK_PSU_ZERO_RPM_DUTY");
        var hydroAsetekProOverrideSafetyProfileFlag = Utils.GetEnvironmentFlag("FANCONTROL_CORSAIRLINK_HYDRO_ASETEK_PRO_SAFETY_PROFILE_OVERRIDE_ENABLED");

        var collection = new List<IDevice>();

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.FlexDongleUsbPowerSupplyUnits)
            .Select(x => new FlexUsbPsuDevice(new FlexDongleUsbPsuProtocol(new FlexSiUsbXpressDevice(x)), deviceGuardManager, new FlexUsbPsuDeviceOptions
            {
                ZeroRpmDutyThreshold = psuZeroRpmDutyThresholdValue,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.FlexModernUsbPowerSupplyUnits)
            .Select(x => new FlexUsbPsuDevice(new ModernPsuProtocol(new FlexSiUsbXpressDevice(x)), deviceGuardManager, new FlexUsbPsuDeviceOptions
            {
                ZeroRpmDutyThreshold = psuZeroRpmDutyThresholdValue,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroAsetekPro2Fan)
            .Select(x => new HydroAsetekProDevice(new AsetekCoolerProtocol(new AsetekSiUsbXpressDevice(x)), deviceGuardManager, new HydroAsetekProDeviceOptions
            {
                FanChannelCount = 2,
                OverrideSafetyProfile = hydroAsetekProOverrideSafetyProfileFlag,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroAsetekPro3Fan)
            .Select(x => new HydroAsetekProDevice(new AsetekCoolerProtocol(new AsetekSiUsbXpressDevice(x)), deviceGuardManager, new HydroAsetekProDeviceOptions
            {
                FanChannelCount = 3,
                OverrideSafetyProfile = hydroAsetekProOverrideSafetyProfileFlag,
            }, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroAsetekVersion1)
            .Select(x => new HydroAsetekDevice(new AsetekCoolerProtocol(new AsetekSiUsbXpressDevice(x)), deviceGuardManager, logger)));

        collection.AddRange(supportedDevices.InDeviceDriverGroup(HardwareIds.DeviceDriverGroups.HydroAsetekVersion2)
            .Select(x => new HydroAsetekDevice(new AsetekCoolerProtocol(new AsetekSiUsbXpressDevice(x)), deviceGuardManager, logger)));

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

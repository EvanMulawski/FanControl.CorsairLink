using HidSharp;

namespace CorsairLink;

public static class DeviceManager
{
    public static SupportedDeviceCollection GetSupportedDevices(ILogger? logger)
    {
        var hidDevices = DeviceList.Local.GetHidDevices(vendorID: HardwareIds.CorsairVendorId);
        var supportedDevices = hidDevices
            .Where(x => HardwareIds.SupportedProductIds.Contains(x.ProductID) && x.GetMaxOutputReportLength() > 0)
            .ToLookup(x => x.ProductID);

        var collection = new SupportedDeviceCollection();

        collection.CommanderProDevices
            .AddRange(supportedDevices[HardwareIds.CorsairCommanderProProductId].Select(x => new CommanderProDevice(x, logger)));

        collection.CommanderProDevices
            .AddRange(supportedDevices[HardwareIds.CorsairObsidian1000DCommanderProProductId].Select(x => new CommanderProDevice(x, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevices[HardwareIds.CorsairCommanderCoreXTProductId].Select(x => new CommanderCoreDevice(x, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevices[HardwareIds.CorsairCommanderCoreProductId].Select(x => new CommanderCoreDevice(x, logger)));

        collection.CommanderCoreDevices
            .AddRange(supportedDevices[HardwareIds.CorsairCommanderSTProductId].Select(x => new CommanderCoreDevice(x, logger)));

        return collection;
    }
}

using HidSharp;

namespace CorsairLink;

public static class DeviceManager
{
    public static SupportedDeviceCollection GetSupportedDevices()
    {
        var hidDevices = DeviceList.Local.GetHidDevices(vendorID: HardwareIds.CorsairVendorId);
        var supportedDevices = hidDevices
            .Where(x => HardwareIds.SupportedProductIds.Contains(x.ProductID))
            .ToLookup(x => x.ProductID);

        var collection = new SupportedDeviceCollection();

        collection.CommanderProDevices.AddRange(supportedDevices[HardwareIds.CorsairCommanderProProductId].Select(x => new CommanderProDevice(x)));
        collection.CommanderProDevices.AddRange(supportedDevices[HardwareIds.CorsairObsidian1000DCommanderProProductId].Select(x => new CommanderProDevice(x)));

        return collection;
    }
}

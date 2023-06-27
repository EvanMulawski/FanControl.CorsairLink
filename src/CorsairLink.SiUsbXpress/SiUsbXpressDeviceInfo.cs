using System;

namespace CorsairLink.SiUsbXpress;

public sealed class SiUsbXpressDeviceInfo : IEquatable<SiUsbXpressDeviceInfo>
{
    public SiUsbXpressDeviceInfo(string devicePath, int vendorId, int productId, string serialNumber, string name)
    {
        DevicePath = devicePath;
        VendorId = vendorId;
        ProductId = productId;
        SerialNumber = serialNumber;
        Name = name;
    }

    public string DevicePath { get; }
    public int VendorId { get; }
    public int ProductId { get; }
    public string SerialNumber { get; }
    public string Name { get; }

    public bool Equals(SiUsbXpressDeviceInfo other)
    {
        return DevicePath == other.DevicePath;
    }

    public override bool Equals(object obj)
    {
        return obj is SiUsbXpressDeviceInfo instance && Equals(instance);
    }

    public override int GetHashCode()
    {
        return DevicePath.GetHashCode();
    }
}

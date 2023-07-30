namespace CorsairLink.Asetek;

public sealed class AsetekDeviceInfo
{
    public AsetekDeviceInfo(string devicePath, int vendorId, int productId, string productName, string serialNumber)
    {
        DevicePath = devicePath;
        VendorId = vendorId;
        ProductId = productId;
        ProductName = productName;
        SerialNumber = serialNumber;
    }

    public string DevicePath { get; }
    public int VendorId { get; }
    public int ProductId { get; }
    public string ProductName { get; }
    public string SerialNumber { get; }
}

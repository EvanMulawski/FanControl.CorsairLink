namespace CorsairLink;

public static class HardwareIds
{
    public static readonly int CorsairVendorId = 0x1b1c;
    public static readonly int CorsairObsidian1000DCommanderProProductId = 0x1d00;
    public static readonly int CorsairCommanderProProductId = 0x0c10;
    public static readonly int CorsairCommanderCoreProductId = 0x0c1c;
    public static readonly int CorsairCommanderCoreXTProductId = 0x0c2a;
    public static readonly int CorsairCommanderSTProductId = 0x0c32;

    public static readonly IReadOnlyCollection<int> SupportedProductIds = new List<int>
    {
        CorsairObsidian1000DCommanderProProductId,
        CorsairCommanderProProductId,
    };
}

namespace CorsairLink;

public static class HardwareIds
{
    public static readonly uint CorsairVendorId = 0x1b1c;
    public static readonly uint CorsairObsidian1000DCommanderProProductId = 0x1d00;
    public static readonly uint CorsairCommanderProProductId = 0x0c10;

    public static readonly IReadOnlyCollection<uint> SupportedProductIds = new List<uint>
    {
        CorsairObsidian1000DCommanderProProductId,
        CorsairCommanderProProductId,
    };
}

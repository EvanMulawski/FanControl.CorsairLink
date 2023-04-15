namespace CorsairLink;

public static class HardwareIds
{
    public static readonly int CorsairVendorId = 0x1b1c;

    public static readonly int CorsairCoolitFamilyProductId = 0x0c04;
    public static readonly int CorsairCommanderProProductId = 0x0c10;
    public static readonly int CorsairHydroH115iPlatinumProductId = 0x0c17;
    public static readonly int CorsairHydroH100iPlatinumProductId = 0x0c18;
    public static readonly int CorsairHydroH100iPlatinumSEProductId = 0x0c19;
    public static readonly int CorsairCommanderCoreProductId = 0x0c1c;
    public static readonly int CorsairHydroH100iProXTProductId = 0x0c20;
    public static readonly int CorsairHydroH115iProXTProductId = 0x0c21;
    public static readonly int CorsairHydroH150iProXTProductId = 0x0c22;
    public static readonly int CorsairHydroH60iProXTProductId = 0x0c29;
    public static readonly int CorsairCommanderCoreXTProductId = 0x0c2a;
    public static readonly int CorsairHydroH100iProXT2ProductId = 0x0c2d;
    public static readonly int CorsairHydroH115iProXT2ProductId = 0x0c2e;
    public static readonly int CorsairHydroH150iProXT2ProductId = 0x0c2f;
    public static readonly int CorsairHydroH60iProXT2ProductId = 0x0c30;
    public static readonly int CorsairCommanderSTProductId = 0x0c32;
    public static readonly int CorsairHydroH60iEliteProductId = 0x0c34;
    public static readonly int CorsairHydroH100iEliteProductId = 0x0c35;
    public static readonly int CorsairHydroH115iEliteProductId = 0x0c36;
    public static readonly int CorsairHydroH150iEliteProductId = 0x0c37;
    public static readonly int CorsairObsidian1000DCommanderProProductId = 0x1d00;

    public static readonly IReadOnlyCollection<int> SupportedProductIds = new List<int>
    {
        // Commander PRO
        CorsairCommanderProProductId,
        CorsairObsidian1000DCommanderProProductId,

        // Commander CORE
        CorsairCommanderCoreProductId,
        CorsairCommanderCoreXTProductId,
        CorsairCommanderSTProductId,

        // Hydro 2 Fan
        CorsairHydroH60iProXTProductId,
        CorsairHydroH100iPlatinumProductId,
        CorsairHydroH100iPlatinumSEProductId,
        CorsairHydroH100iProXTProductId,
        CorsairHydroH100iEliteProductId,
        CorsairHydroH115iProXTProductId,
        CorsairHydroH115iPlatinumProductId,
        CorsairHydroH100iProXT2ProductId,
        CorsairHydroH115iProXT2ProductId,
        CorsairHydroH60iProXT2ProductId,
        CorsairHydroH60iEliteProductId,
        CorsairHydroH115iEliteProductId,

        // Hydro 3 Fan
        CorsairHydroH150iEliteProductId,
        CorsairHydroH150iProXTProductId,
        CorsairHydroH150iProXT2ProductId,

        // CoolIT Product Family
        CorsairCoolitFamilyProductId,
    };

    public static class DeviceDriverGroups
    {
        public static readonly IReadOnlyCollection<int> CommanderPro = new List<int>
        {
            CorsairCommanderProProductId,
            CorsairObsidian1000DCommanderProProductId,
        };

        public static readonly IReadOnlyCollection<int> CommanderCore = new List<int>
        {
            CorsairCommanderCoreXTProductId,
        };

        public static readonly IReadOnlyCollection<int> CommanderCoreWithDesignatedPump = new List<int>
        {
            CorsairCommanderCoreProductId,
            CorsairCommanderSTProductId,
        };

        public static readonly IReadOnlyCollection<int> Hydro2Fan = new List<int>
        {
            CorsairHydroH60iProXTProductId,
            CorsairHydroH100iPlatinumProductId,
            CorsairHydroH100iPlatinumSEProductId,
            CorsairHydroH100iProXTProductId,
            CorsairHydroH100iEliteProductId,
            CorsairHydroH115iProXTProductId,
            CorsairHydroH115iPlatinumProductId,
            CorsairHydroH100iProXT2ProductId,
            CorsairHydroH115iProXT2ProductId,
            CorsairHydroH60iProXT2ProductId,
            CorsairHydroH60iEliteProductId,
            CorsairHydroH115iEliteProductId,
        };

        public static readonly IReadOnlyCollection<int> Hydro3Fan = new List<int>
        {
            CorsairHydroH150iEliteProductId,
            CorsairHydroH150iProXTProductId,
            CorsairHydroH150iProXT2ProductId,
        };

        public static readonly IReadOnlyCollection<int> CoolitFamily = new List<int>
        {
            CorsairCoolitFamilyProductId,
        };
    }
}

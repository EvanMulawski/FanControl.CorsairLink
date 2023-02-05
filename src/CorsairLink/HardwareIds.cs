namespace CorsairLink;

public static class HardwareIds
{
    public static readonly int CorsairVendorId = 0x1b1c;

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
    public static readonly int Corsair0c2dProductId = 0x0c2d; // Platinum XT Family (2 Fan), H100iRGBPROXT ?
    public static readonly int Corsair0c2eProductId = 0x0c2e; // Platinum XT Family (2 Fan)
    public static readonly int Corsair0c2fProductId = 0x0c2f; // HydroH150iXT Family (3 Fan)
    public static readonly int Corsair0c30ProductId = 0x0c30; // Platinum XT Family (2 Fan)
    public static readonly int CorsairCommanderSTProductId = 0x0c32;
    public static readonly int Corsair0c34ProductId = 0x0c34; // Tamriel Family (2 Fan)
    public static readonly int CorsairHydroH100iEliteProductId = 0x0c35;
    public static readonly int Corsair0c36ProductId = 0x0c36; // Tamriel Family (2 Fan), HydroH115iElite ?
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
        Corsair0c2dProductId,
        Corsair0c2eProductId,
        Corsair0c30ProductId,
        Corsair0c34ProductId,
        Corsair0c36ProductId,

        // Hydro 3 Fan
        CorsairHydroH150iProXTProductId,
        Corsair0c2fProductId,
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
            Corsair0c2dProductId,
            Corsair0c2eProductId,
            Corsair0c30ProductId,
            Corsair0c34ProductId,
            Corsair0c36ProductId,
        };

        public static readonly IReadOnlyCollection<int> Hydro3Fan = new List<int>
        {
            CorsairHydroH150iProXTProductId,
            Corsair0c2fProductId,
        };
    }
}

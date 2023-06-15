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
    public static readonly int CorsairPsuHX550iProductId = 0x1c03;
    public static readonly int CorsairPsuHX650iProductId = 0x1c04;
    public static readonly int CorsairPsuHX750iProductId = 0x1c05;
    public static readonly int CorsairPsuHX850iProductId = 0x1c06;
    public static readonly int CorsairPsuHX1000iProductId = 0x1c07;
    public static readonly int CorsairPsuHX1200iProductId = 0x1c08;
    public static readonly int CorsairPsuHX1000i2021ProductId = 0x1c1e;
    public static readonly int CorsairPsuHX1500i2021ProductId = 0x1c1f;
    public static readonly int CorsairPsuRM550iProductId = 0x1c09;
    public static readonly int CorsairPsuRM650iProductId = 0x1c0a;
    public static readonly int CorsairPsuRM750iProductId = 0x1c0b;
    public static readonly int CorsairPsuRM850iProductId = 0x1c0c;
    public static readonly int CorsairPsuRM1000iProductId = 0x1c0d;
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

        // HID PSU
        CorsairPsuHX550iProductId,
        CorsairPsuHX650iProductId,
        CorsairPsuHX750iProductId,
        CorsairPsuHX850iProductId,
        CorsairPsuHX1000iProductId,
        CorsairPsuHX1200iProductId,
        CorsairPsuHX1000i2021ProductId,
        CorsairPsuHX1500i2021ProductId,
        CorsairPsuRM550iProductId,
        CorsairPsuRM650iProductId,
        CorsairPsuRM750iProductId,
        CorsairPsuRM850iProductId,
        CorsairPsuRM1000iProductId,
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

        public static readonly IReadOnlyCollection<int> PowerSupplyUnits = new List<int>
        {
            CorsairPsuHX550iProductId,
            CorsairPsuHX650iProductId,
            CorsairPsuHX750iProductId,
            CorsairPsuHX850iProductId,
            CorsairPsuHX1000iProductId,
            CorsairPsuHX1200iProductId,
            CorsairPsuHX1000i2021ProductId,
            CorsairPsuHX1500i2021ProductId,
            CorsairPsuRM550iProductId,
            CorsairPsuRM650iProductId,
            CorsairPsuRM750iProductId,
            CorsairPsuRM850iProductId,
            CorsairPsuRM1000iProductId,
        };
    }
}

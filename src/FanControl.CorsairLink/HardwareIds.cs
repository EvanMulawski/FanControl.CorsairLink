namespace CorsairLink;

public static class HardwareIds
{
    public static readonly int CorsairVendorId = 0x1b1c;

    public static readonly int CorsairH80iGTProductId = 0x0c02;
    public static readonly int CorsairH100iGTXProductId = 0x0c03;
    public static readonly int CorsairCoolitFamilyProductId = 0x0c04;
    public static readonly int CorsairH110iGTXProductId = 0x0c07;
    public static readonly int CorsairH80iGTv2ProductId = 0x0c08;
    public static readonly int CorsairH100iGTv2ProductId = 0x0c09;
    public static readonly int CorsairH110iGTv2ProductId = 0x0c0a;
    public static readonly int CorsairCommanderProProductId = 0x0c10;
    public static readonly int CorsairHydroH150iProProductId = 0x0c12;
    public static readonly int CorsairHydroH115iProProductId = 0x0c13;
    public static readonly int CorsairOneProductId = 0x0c14;
    public static readonly int CorsairHydroH100iProProductId = 0x0c15;
    public static readonly int CorsairHydroH80iProProductId = 0x0c16;
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
    public static readonly int CorsairCommanderCore2022ProductId = 0x0c32;
    public static readonly int CorsairHydroH60iEliteProductId = 0x0c34;
    public static readonly int CorsairHydroH100iEliteBlackProductId = 0x0c35;
    public static readonly int CorsairHydroH115iEliteProductId = 0x0c36;
    public static readonly int CorsairHydroH150iEliteBlackProductId = 0x0c37;
    public static readonly int CorsairICueLinkHubProductId = 0x0c3f;
    public static readonly int CorsairHydroH100iEliteWhiteProductId = 0x0c40;
    public static readonly int CorsairHydroH150iEliteWhiteProductId = 0x0c41;
    public static readonly int CorsairXc7LcdWaterBlockProductId = 0x0c42;
    public static readonly int CorsairPsuAXiDongleFamilyProductId = 0x1c00;
    public static readonly int CorsairPsuAX1500iProductId = 0x1c02;
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
    public static readonly int CorsairPsuAX850iProductId = 0x1c0e;
    public static readonly int CorsairPsuAX1000iProductId = 0x1c0f;
    public static readonly int CorsairPsuAX1300iProductId = 0x1c10;
    public static readonly int CorsairPsuAX1600iProductId = 0x1c11;
    public static readonly int CorsairPsuHX1200i2023ProductId = 0x1c23;
    public static readonly int CorsairPsuHX1200i2025ProductId = 0x1c27;
    public static readonly int CorsairObsidian1000DCommanderProProductId = 0x1d00;

    public static class DeviceDriverGroups
    {
        public static readonly IReadOnlyCollection<int> CommanderPro = new List<int>
        {
            CorsairCommanderProProductId,
            CorsairObsidian1000DCommanderProProductId,
        };

        public static readonly IReadOnlyCollection<int> ICueLinkHub = new List<int>
        {
            CorsairICueLinkHubProductId,
        };

        public static readonly IReadOnlyCollection<int> CommanderCore = new List<int>
        {
            CorsairCommanderCoreXTProductId,
        };

        public static readonly IReadOnlyCollection<int> CommanderCoreWithDesignatedPump = new List<int>
        {
            CorsairCommanderCoreProductId,
            CorsairCommanderCore2022ProductId,
        };

        public static readonly IReadOnlyCollection<int> HydroPlatinum2Fan = new List<int>
        {
            CorsairHydroH60iProXTProductId,
            CorsairHydroH100iPlatinumProductId,
            CorsairHydroH100iPlatinumSEProductId,
            CorsairHydroH100iProXTProductId,
            CorsairHydroH100iEliteBlackProductId,
            CorsairHydroH100iEliteWhiteProductId,
            CorsairHydroH115iProXTProductId,
            CorsairHydroH115iPlatinumProductId,
            CorsairHydroH100iProXT2ProductId,
            CorsairHydroH115iProXT2ProductId,
            CorsairHydroH60iProXT2ProductId,
            CorsairHydroH60iEliteProductId,
            CorsairHydroH115iEliteProductId,
        };

        public static readonly IReadOnlyCollection<int> HydroPlatinum3Fan = new List<int>
        {
            CorsairHydroH150iEliteBlackProductId,
            CorsairHydroH150iEliteWhiteProductId,
            CorsairHydroH150iProXTProductId,
            CorsairHydroH150iProXT2ProductId,
        };

        public static readonly IReadOnlyCollection<int> CoolitFamily = new List<int>
        {
            CorsairCoolitFamilyProductId,
        };

        public static readonly IReadOnlyCollection<int> HidPowerSupplyUnits = new List<int>
        {
            CorsairPsuHX550iProductId,
            CorsairPsuHX650iProductId,
            CorsairPsuHX750iProductId,
            CorsairPsuHX850iProductId,
            CorsairPsuHX1000iProductId,
            CorsairPsuHX1200iProductId,
            CorsairPsuHX1200i2023ProductId,
            CorsairPsuHX1200i2025ProductId,
            CorsairPsuHX1000i2021ProductId,
            CorsairPsuHX1500i2021ProductId,
            CorsairPsuRM550iProductId,
            CorsairPsuRM650iProductId,
            CorsairPsuRM750iProductId,
            CorsairPsuRM850iProductId,
            CorsairPsuRM1000iProductId,
        };

        public static readonly IReadOnlyCollection<int> FlexDongleUsbPowerSupplyUnits = new List<int>
        {
            CorsairPsuAXiDongleFamilyProductId,
            CorsairPsuAX1500iProductId,
        };

        public static readonly IReadOnlyCollection<int> FlexModernUsbPowerSupplyUnits = new List<int>
        {
            CorsairPsuAX850iProductId,
            CorsairPsuAX1000iProductId,
            CorsairPsuAX1300iProductId,
            CorsairPsuAX1600iProductId,
        };

        public static readonly IReadOnlyCollection<int> HydroAsetekPro2Fan = new List<int>
        {
            CorsairHydroH115iProProductId,
            CorsairHydroH100iProProductId,
            CorsairHydroH80iProProductId,
        };

        public static readonly IReadOnlyCollection<int> HydroAsetekPro3Fan = new List<int>
        {
            CorsairHydroH150iProProductId,
        };

        public static readonly IReadOnlyCollection<int> HydroAsetekVersion1 = new List<int>
        {
            CorsairH80iGTProductId,
            CorsairH100iGTXProductId,
            CorsairH110iGTXProductId,
        };

        public static readonly IReadOnlyCollection<int> HydroAsetekVersion2 = new List<int>
        {
            CorsairH80iGTv2ProductId,
            CorsairH100iGTv2ProductId,
            CorsairH110iGTv2ProductId,
        };

        public static readonly IReadOnlyCollection<int> Xc7 = new List<int>
        {
            CorsairXc7LcdWaterBlockProductId,
        };

        public static readonly IReadOnlyCollection<int> One = new List<int>
        {
            CorsairOneProductId,
        };
    }

    public static IReadOnlyCollection<int> GetSupportedProductIds() =>
        DeviceDriverGroups.CommanderPro
        .Concat(DeviceDriverGroups.ICueLinkHub)
        .Concat(DeviceDriverGroups.CommanderCore)
        .Concat(DeviceDriverGroups.CommanderCoreWithDesignatedPump)
        .Concat(DeviceDriverGroups.HydroPlatinum2Fan)
        .Concat(DeviceDriverGroups.HydroPlatinum3Fan)
        .Concat(DeviceDriverGroups.CoolitFamily)
        .Concat(DeviceDriverGroups.HidPowerSupplyUnits)
        .Concat(DeviceDriverGroups.FlexDongleUsbPowerSupplyUnits)
        .Concat(DeviceDriverGroups.FlexModernUsbPowerSupplyUnits)
        .Concat(DeviceDriverGroups.HydroAsetekPro2Fan)
        .Concat(DeviceDriverGroups.HydroAsetekPro3Fan)
        .Concat(DeviceDriverGroups.HydroAsetekVersion1)
        .Concat(DeviceDriverGroups.HydroAsetekVersion2)
        .Concat(DeviceDriverGroups.Xc7)
        .Concat(DeviceDriverGroups.One)
        .ToList();
}

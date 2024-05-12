namespace CorsairLink.Devices.HydroPlatinum;

internal static class Commands
{
    public static readonly byte IncomingState = 0x00;
    public static readonly byte Cooling = 0x00;
    public static readonly byte DirectLightingConfiguration = 0x01;
    public static readonly byte LightingIndexes = 0x02;
    public static readonly byte CoolingThreeFanPacket = 0x03;
    public static readonly byte LightingColors = 0x04;
}

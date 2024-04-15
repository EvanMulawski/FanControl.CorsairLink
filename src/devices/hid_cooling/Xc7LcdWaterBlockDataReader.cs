using System.Buffers.Binary;

namespace CorsairLink.Devices.HidCooling;

public static class Xc7LcdWaterBlockDataReader
{
    public static string GetFirmwareVersion(ReadOnlySpan<byte> packet)
    {
        return Utils.ParseString(packet.Slice(6));
    }

    public static float GetLiquidTemperature(ReadOnlySpan<byte> packet)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(2, 2)) / 10f;
    }
}
